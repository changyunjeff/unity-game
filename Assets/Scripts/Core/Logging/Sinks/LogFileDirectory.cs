using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Game.Core.Logging.Sinks
{
    /// <summary>
    /// 日志目录的规矩：每次运行一个文件，名字是 <c>log-年月日-时分秒.txt</c>；只留最近几次。
    /// 它只管起名字和清旧文件，不管往文件里写什么。
    /// </summary>
    public sealed class LogFileDirectory
    {
        private const string FilePrefix = "log-";
        private const string FileExtension = ".txt";

        public LogFileDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("日志目录不能为空", nameof(path));
            }

            Path = path;
        }

        public string Path { get; }

        /// <summary>给这次运行起一个还不存在的文件名。目录不存在会建。</summary>
        public string NewSessionFilePath(DateTime sessionStart)
        {
            Directory.CreateDirectory(Path);

            string name = FilePrefix + sessionStart.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string filePath = System.IO.Path.Combine(Path, name + FileExtension);

            // 同一秒里起了两次：后面的加序号。
            for (int i = 2; File.Exists(filePath); i++)
            {
                filePath = System.IO.Path.Combine(Path, name + "-" + i.ToString(CultureInfo.InvariantCulture) + FileExtension);
            }

            return filePath;
        }

        /// <summary>
        /// 只留最近 <paramref name="keep"/> 次运行的文件，更早的删掉。
        /// 删不掉的（正被别的程序占着）不抛异常，把原因逐条返回，由调用的人决定怎么报。
        /// </summary>
        public IReadOnlyList<string> DeleteOldSessions(int keep)
        {
            if (keep <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keep));
            }

            var failures = new List<string>();
            if (!Directory.Exists(Path))
            {
                return failures;
            }

            // 文件名里的时间按字典序排，排完最后几个就是最近几次。
            string[] files = Directory.GetFiles(Path, FilePrefix + "*" + FileExtension);
            Array.Sort(files, StringComparer.Ordinal);

            for (int i = 0; i < files.Length - keep; i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    failures.Add("没能删掉旧日志 " + System.IO.Path.GetFileName(files[i]) + "：" + exception.Message);
                }
            }

            return failures;
        }
    }
}
