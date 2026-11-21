using System;
using System.Collections.Generic;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>一个崩溃包刚写完的通知：为什么写、写在哪、里面有哪些文件。</summary>
    public readonly struct CrashReportWritten
    {
        /// <summary>原因，见 <see cref="CrashReasons"/>。</summary>
        public readonly string Reason;

        /// <summary>崩溃包的目录。</summary>
        public readonly string Directory;

        /// <summary>包里的文件，相对于 <see cref="Directory"/> 的路径。</summary>
        public readonly IReadOnlyList<string> Files;

        public CrashReportWritten(string reason, string directory, IReadOnlyList<string> files)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("崩溃包的目录不能为空", nameof(directory));
            }

            Reason = reason;
            Directory = directory;
            Files = files ?? Array.Empty<string>();
        }
    }
}
