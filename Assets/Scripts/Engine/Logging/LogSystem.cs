using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Logging;
using Game.Core.Logging.CrashReports;
using Game.Core.Logging.Pipeline;
using Game.Core.Logging.Sinks;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 日志系统：把核心层的零件按设定组装起来，装到 <see cref="Log"/> 上；<see cref="Dispose"/> 时按相反的先后拆掉。
    /// 它只做组装，每件事怎么做都在各个零件里。
    ///
    /// 组装出来的样子：
    /// <code>
    /// Log ─► 管线（盖戳 → 同一个错只记一次 → 补调用栈）─► 多个去处 ┬ 内存里的最近记录
    ///                                                           ├ Unity 的 Console
    ///                                                           ├ 日志文件
    ///                                                           └ 盯着没人接住的异常（触发崩溃包）
    /// </code>
    /// 每写完一个崩溃包，都把 <see cref="CrashReportWritten"/> 交给建它时给的通知（崩溃界面听这个）。
    /// </summary>
    public sealed class LogSystem : IDisposable
    {
        private static readonly LogCategory LogLogging = new LogCategory("Log");
        private static readonly LogCategory LogCrash = new LogCategory("Crash");

        private readonly LogSystemSettings _settings;
        private readonly IReadOnlyList<string> _sessionInfo;
        private readonly ILogSink _outputBefore;
        private readonly UnityConsoleBridge _console;
        private readonly UnhandledExceptionCapture _unhandledExceptions;
        private readonly CrashReportStore _crashReports;
        private readonly FileLogSink _file;
        private readonly UncleanExitMarker _runningMarker;
        private readonly Action<CrashReportWritten> _onCrashReportWritten;
        private bool _disposed;

        private LogSystem(LogSystemSettings settings, IReadOnlyList<string> sessionInfo, Action<CrashReportWritten> onCrashReportWritten)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sessionInfo = sessionInfo ?? Array.Empty<string>();
            _onCrashReportWritten = onCrashReportWritten ?? throw new ArgumentNullException(nameof(onCrashReportWritten));

            // 1. 去处：先装不会出错的两个，这样后面每一步出了问题都有地方记。
            Recent = new RecentLogBuffer();
            _console = new UnityConsoleBridge();
            Sinks = new CompositeLogSink(Recent, _console);

            // 2. 管线装到入口上。从这一行起，Log.Xxx 就有输出了。
            _outputBefore = Log.Output;
            Log.Output = new LogPipeline(new UnityLogClock(), Sinks, new RepeatedErrorFilter(), new CallSiteStackEnricher());

            // 3. 日志文件。建不起来不算致命：记一条错误，这次运行少一个去处。
            _file = OpenLogFile();

            // 4. 兜住不是我们自己打的：Unity 报的日志、别的线程上没人接住的异常。
            _console.StartListening();
            _unhandledExceptions = new UnhandledExceptionCapture();

            // 5. 崩溃包。
            _crashReports = new CrashReportStore(
                settings.CrashReportDirectory, Scrubber(settings.UserProfilePath), PlainTextLogFormatter.Instance, settings.KeepCrashReports);

            if (settings.ReportOnException)
            {
                Sinks.Add(new FirstExceptionSink(ReportException));
            }

            if (settings.DetectUncleanExit)
            {
                _runningMarker = new UncleanExitMarker(settings.RunningMarkerPath);
                ReportUncleanExitIfAny();
                _runningMarker.Set(CurrentLogFile);
            }
        }

        /// <summary>内存里的最近记录。</summary>
        public RecentLogBuffer Recent { get; }

        /// <summary>所有的去处。要加自己的去处，往这里加。</summary>
        public CompositeLogSink Sinks { get; }

        /// <summary>这次运行的日志文件；没建起来时是 null。</summary>
        public string CurrentLogFile => _file?.FilePath;

        /// <summary>建一个日志系统并装到 <see cref="Log"/> 上。要在主线程上调用。</summary>
        /// <param name="onCrashReportWritten">每写完一个崩溃包调一次。可能在任何线程上被调用；启动时补写的"上次没正常退出"的包，在这个函数返回之前就会通知。</param>
        public static LogSystem Start(LogSystemSettings settings, IReadOnlyList<string> sessionInfo, Action<CrashReportWritten> onCrashReportWritten)
        {
            return new LogSystem(settings, sessionInfo, onCrashReportWritten);
        }

        /// <summary>手动写一个崩溃包，带上这次运行的信息、最近的日志和设定里要带的文件。返回包的目录。</summary>
        public string WriteCrashReport(string reason, params string[] details)
        {
            var report = new CrashReport(reason, DateTime.Now) { RecentEntries = Recent.Lines() };
            report.Details.Add("原因：" + reason);
            report.Details.AddRange(details);
            report.Details.Add("");
            report.Details.AddRange(_sessionInfo);
            report.Attachments.AddRange(_settings.CrashReportAttachments);

            string directory = _crashReports.Save(report);
            Log.Info(LogCrash, "崩溃包已写到 " + directory);
            Notify(reason, directory);
            return directory;
        }

        /// <summary>拆掉：先后和组装时相反。之后 <see cref="Log"/> 的输出回到组装之前的样子。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // 走到这里就是正常退出，拿走"正在运行"的标记。
            _runningMarker?.Clear();
            _unhandledExceptions.Dispose();
            _console.Dispose();
            Log.Output = _outputBefore;
            _file?.Dispose();
        }

        private FileLogSink OpenLogFile()
        {
            try
            {
                var directory = new LogFileDirectory(_settings.LogDirectory);
                var file = new FileLogSink(
                    directory.NewSessionFilePath(DateTime.Now), PlainTextLogFormatter.Instance, _sessionInfo, _settings.MaxLogFileBytes);
                Sinks.Add(file);

                foreach (string failure in directory.DeleteOldSessions(_settings.KeepLogSessions))
                {
                    Log.Warning(LogLogging, failure);
                }

                return file;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Log.Exception(LogLogging, exception, "日志文件建不起来，这次运行只有 Console 和内存里的记录");
                return null;
            }
        }

        private static ITextScrubber Scrubber(string userProfilePath)
        {
            return string.IsNullOrEmpty(userProfilePath) ? (ITextScrubber)NullTextScrubber.Instance : new PathScrubber(userProfilePath);
        }

        private void Notify(string reason, string directory)
        {
            _onCrashReportWritten(new CrashReportWritten(reason, directory, CrashReportStore.ListFiles(directory)));
        }

        // 这次运行里第一次出现没人接住的异常。
        private void ReportException(LogEntry entry)
        {
            WriteCrashReport(CrashReasons.Exception, "异常：" + entry.Message, entry.StackTrace ?? "（没有调用栈）");
        }

        // 上次的"正在运行"标记还在：上次没走到正常退出。用上次的日志文件和 Unity 留下的崩溃目录补写一个崩溃包。
        private void ReportUncleanExitIfAny()
        {
            if (!_runningMarker.TryReadLeftover(out string previousLogFile, out DateTime previousStart))
            {
                return;
            }

            var report = new CrashReport(CrashReasons.UncleanExit, DateTime.Now);
            report.Details.Add("原因：上次运行没有正常退出");
            report.Details.Add("上次的日志：" + previousLogFile);
            report.Attachments.AddRange(_settings.CrashReportAttachments);

            if (!string.IsNullOrEmpty(previousLogFile))
            {
                report.Attachments.Add(previousLogFile);
            }

            // 进程直接崩掉时 Unity 自己会留一个带内存转储的目录。上次运行开始之后出现的，都是上次留下的。
            IReadOnlyList<string> unityCrashes = new CrashDumpFinder(_settings.UnityCrashDirectory).FoldersSince(previousStart);
            report.Details.Add("Unity 的崩溃目录：" + (unityCrashes.Count == 0 ? "没有找到" : "找到 " + unityCrashes.Count + " 个"));
            report.AttachedDirectories.AddRange(unityCrashes);

            string directory = _crashReports.Save(report);
            Log.Warning(LogCrash, "上次运行没有正常退出，崩溃包已写到 " + directory);
            Notify(CrashReasons.UncleanExit, directory);
        }
    }
}
