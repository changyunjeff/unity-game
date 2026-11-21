using System;
using System.IO;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 玩家选过"以后都不问"的记号：一个文件，在就是选过。跨运行保留。
    /// 用文件而不用 Unity 的 PlayerPrefs，是为了留在核心层、能测，而且和崩溃包放在同一个目录下，玩家想反悔删掉它就行。
    /// </summary>
    public sealed class CrashPromptOptOut
    {
        private readonly string _path;

        public CrashPromptOptOut(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("记号文件的路径不能为空", nameof(path));
            }

            _path = path;
        }

        /// <summary>玩家有没有选过"以后都不问"。</summary>
        public bool IsOptedOut => File.Exists(_path);

        /// <summary>记下"以后都不问"。</summary>
        public void OptOut()
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_path, "玩家在崩溃界面上选了“以后都不问”。删掉这个文件就会重新问。");
        }
    }
}
