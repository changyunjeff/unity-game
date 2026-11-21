using System;
using System.Collections.Generic;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 一份崩溃报告要装的东西。先把它填好，再交给 <see cref="CrashReportStore"/> 存成一个目录。
    /// </summary>
    public sealed class CrashReport
    {
        /// <param name="reason">原因，会成为目录名的一部分，例如 Exception、UncleanExit、Manual。</param>
        /// <param name="time">出事的时间，用来起目录名。</param>
        public CrashReport(string reason, DateTime time)
        {
            Reason = reason;
            Time = time;
        }

        public string Reason { get; }

        public DateTime Time { get; }

        /// <summary>写进 info.txt 的内容：出了什么事、版本、机器信息。</summary>
        public List<string> Details { get; } = new List<string>();

        /// <summary>出事前的日志，写进 log.txt。</summary>
        public IReadOnlyList<LogEntry> RecentEntries { get; set; } = Array.Empty<LogEntry>();

        /// <summary>要一起带走的文本文件的路径。不存在的会在 info.txt 里注明。</summary>
        public List<string> Attachments { get; } = new List<string>();

        /// <summary>
        /// 要整个带走的目录的路径，例如 Unity 自己留下的崩溃目录。放进包里时保留目录名。
        /// 里面的文本文件做隐私处理，别的文件（内存转储）原样复制。不存在的会在 info.txt 里注明。
        /// </summary>
        public List<string> AttachedDirectories { get; } = new List<string>();
    }
}
