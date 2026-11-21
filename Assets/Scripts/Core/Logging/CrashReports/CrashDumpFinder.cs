using System;
using System.Collections.Generic;
using System.IO;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 找 Unity 自己留下的崩溃目录。进程直接崩掉时，Unity 的崩溃处理会在一个固定的目录下
    /// 为每次崩溃建一个子目录，里面有内存转储（crash.dmp）、error.log 和那次的 Player.log。
    /// 这里只认时间：在某个时刻之后出现或改过的子目录，算那之后的崩溃留下的。
    /// </summary>
    public sealed class CrashDumpFinder
    {
        private readonly string _rootDirectory;

        /// <param name="rootDirectory">Unity 放崩溃目录的地方。为空表示这个平台上不收。</param>
        public CrashDumpFinder(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        /// <summary>在 <paramref name="since"/> 当时或之后留下的崩溃目录，旧的在前。目录不存在时是空的。</summary>
        public IReadOnlyList<string> FoldersSince(DateTime since)
        {
            if (string.IsNullOrEmpty(_rootDirectory) || !Directory.Exists(_rootDirectory))
            {
                return Array.Empty<string>();
            }

            var found = new List<string>();
            foreach (string directory in Directory.GetDirectories(_rootDirectory))
            {
                if (Directory.GetLastWriteTime(directory) >= since)
                {
                    found.Add(directory);
                }
            }

            found.Sort(StringComparer.Ordinal);
            return found;
        }
    }
}
