using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core.Logging.Sinks;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 崩溃包存放的地方。一份报告存成一个目录，名字是 <c>年月日-时分秒-原因</c>，里面是：
    /// <c>info.txt</c>（出了什么事）、<c>log.txt</c>（出事前的日志）、以及报告要带走的文件和目录。
    /// 写进去的每一行文字都先做隐私处理；内存转储这类二进制文件做不了，原样复制。只留最近若干个。
    /// </summary>
    public sealed class CrashReportStore
    {
        public const string InfoFileName = "info.txt";
        public const string LogFileName = "log.txt";

        // 按文本处理（逐行做隐私处理）的扩展名。别的一律当二进制原样复制。
        private static readonly string[] TextExtensions = { ".txt", ".log", ".json", ".xml" };

        private readonly ITextScrubber _scrubber;
        private readonly ILogFormatter _formatter;
        private readonly int _keep;

        public CrashReportStore(string rootDirectory, ITextScrubber scrubber, ILogFormatter formatter, int keep = 10)
        {
            if (string.IsNullOrEmpty(rootDirectory))
            {
                throw new ArgumentException("崩溃包目录不能为空", nameof(rootDirectory));
            }

            if (keep <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keep));
            }

            RootDirectory = rootDirectory;
            _scrubber = scrubber ?? throw new ArgumentNullException(nameof(scrubber));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _keep = keep;
        }

        public string RootDirectory { get; }

        /// <summary>把一份报告存成一个目录，返回这个目录。</summary>
        public string Save(CrashReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            Directory.CreateDirectory(RootDirectory);
            string directory = NewDirectory(report);

            var info = new List<string>(report.Details);
            foreach (string attachment in report.Attachments)
            {
                info.Add(CopyAttachment(attachment, directory));
            }

            foreach (string attachedDirectory in report.AttachedDirectories)
            {
                info.Add(CopyAttachedDirectory(attachedDirectory, directory));
            }

            var log = new List<string>(report.RecentEntries.Count);
            for (int i = 0; i < report.RecentEntries.Count; i++)
            {
                LogEntry entry = report.RecentEntries[i];
                log.Add(_formatter.Format(in entry));
            }

            WriteScrubbed(Path.Combine(directory, InfoFileName), info);
            WriteScrubbed(Path.Combine(directory, LogFileName), log);

            DeleteOldReports();
            return directory;
        }

        private string NewDirectory(CrashReport report)
        {
            string name = report.Time.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + SafeName(report.Reason);
            string path = Path.Combine(RootDirectory, name);

            // 同一秒里同一个原因存了两次：后面的加序号。
            for (int i = 2; Directory.Exists(path); i++)
            {
                path = Path.Combine(RootDirectory, name + "-" + i.ToString(CultureInfo.InvariantCulture));
            }

            Directory.CreateDirectory(path);
            return path;
        }

        // 原因要当目录名用：只留字母、数字、下划线和横线，别的换成下划线。
        private static string SafeName(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "Unknown";
            }

            var builder = new StringBuilder(reason.Length);
            foreach (char c in reason)
            {
                bool safe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                builder.Append(safe ? c : '_');
            }

            return builder.ToString();
        }

        /// <summary>一个崩溃包目录里的所有文件，相对于这个目录的路径，按字典序。</summary>
        public static IReadOnlyList<string> ListFiles(string reportDirectory)
        {
            string[] files = Directory.GetFiles(reportDirectory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetRelativePath(reportDirectory, files[i]);
            }

            Array.Sort(files, StringComparer.Ordinal);
            return files;
        }

        // 把一个文本文件逐行做完隐私处理再放进崩溃包。返回一行说明，写进 info.txt。
        private string CopyAttachment(string source, string directory)
        {
            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                return "没有带上：" + source + "（文件不存在）";
            }

            string name = Path.GetFileName(source);
            CopyScrubbed(source, Path.Combine(directory, name));
            return "带上了：" + name;
        }

        // 把一个目录整个放进崩溃包，保留目录名和里面的层次。返回一行说明，写进 info.txt。
        private string CopyAttachedDirectory(string source, string directory)
        {
            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                return "没有带上：" + source + "（目录不存在）";
            }

            string name = Path.GetFileName(source.TrimEnd('\\', '/'));
            string target = Path.Combine(directory, name);

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));

                if (IsText(file))
                {
                    CopyScrubbed(file, destination);
                }
                else
                {
                    CopyRaw(file, destination);
                }
            }

            return "带上了目录：" + name;
        }

        private static bool IsText(string path)
        {
            string extension = Path.GetExtension(path);
            foreach (string text in TextExtensions)
            {
                if (string.Equals(extension, text, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // 源文件多半还开着在写（当前的日志、Unity 的 Player.log），所以要允许别人同时读写。
        private static FileStream OpenShared(string source)
        {
            return new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        private void CopyScrubbed(string source, string destination)
        {
            using (FileStream input = OpenShared(source))
            using (var reader = new StreamReader(input, Encoding.UTF8))
            using (var writer = new StreamWriter(destination, false, new UTF8Encoding(false)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.Write(_scrubber.Scrub(line));
                    writer.Write('\n');
                }
            }
        }

        private static void CopyRaw(string source, string destination)
        {
            using (FileStream input = OpenShared(source))
            using (FileStream output = File.Create(destination))
            {
                input.CopyTo(output);
            }
        }

        private void WriteScrubbed(string path, List<string> lines)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                foreach (string line in lines)
                {
                    writer.Write(_scrubber.Scrub(line));
                    writer.Write('\n');
                }
            }
        }

        // 目录名以时间开头，按字典序排完，最后几个就是最近几个。
        private void DeleteOldReports()
        {
            string[] directories = Directory.GetDirectories(RootDirectory);
            Array.Sort(directories, StringComparer.Ordinal);

            for (int i = 0; i < directories.Length - _keep; i++)
            {
                Directory.Delete(directories[i], true);
            }
        }
    }
}
