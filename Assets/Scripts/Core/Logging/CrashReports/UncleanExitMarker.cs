using System;
using System.IO;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 用一个标记文件判断"上次运行有没有正常退出"。
    /// 启动时放下标记，正常退出时拿走。下次启动发现标记还在，就说明上次没走到正常退出那一步
    /// （崩溃、被强行结束、断电）。标记里可以留一句话，例如上次的日志文件在哪。
    /// </summary>
    public sealed class UncleanExitMarker
    {
        private readonly string _path;

        public UncleanExitMarker(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("标记文件的路径不能为空", nameof(path));
            }

            _path = path;
        }

        /// <summary>上次留下的标记还在不在。在的话，<paramref name="note"/> 是上次留在里面的那句话。</summary>
        public bool TryReadLeftover(out string note) => TryReadLeftover(out note, out _);

        /// <summary>
        /// 同上，再加上那个标记是什么时候放下的，也就是上次运行开始的时候。
        /// 用它来认哪些东西是上次运行留下的（例如 Unity 的崩溃目录）。
        /// </summary>
        public bool TryReadLeftover(out string note, out DateTime setTime)
        {
            if (!File.Exists(_path))
            {
                note = null;
                setTime = default;
                return false;
            }

            note = File.ReadAllText(_path).Trim();
            setTime = File.GetLastWriteTime(_path);
            return true;
        }

        /// <summary>放下这次运行的标记，盖掉上次留下的。</summary>
        public void Set(string note)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_path, note ?? "");
        }

        /// <summary>正常退出时拿走标记。</summary>
        public void Clear() => File.Delete(_path);
    }
}
