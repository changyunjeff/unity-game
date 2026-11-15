using System;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 把文字里玩家的用户目录换成 <c>~</c>，这样崩溃包里不会带着玩家的系统用户名。
    /// 例如 <c>C:\Users\zhangsan\AppData\...</c> 变成 <c>~\AppData\...</c>。
    /// </summary>
    public sealed class PathScrubber : ITextScrubber
    {
        private readonly string _backslashed;
        private readonly string _slashed;

        /// <param name="userProfilePath">玩家的用户目录，例如 <c>C:\Users\zhangsan</c>。</param>
        public PathScrubber(string userProfilePath)
        {
            if (string.IsNullOrEmpty(userProfilePath))
            {
                throw new ArgumentException("用户目录不能为空。不需要隐私处理时用 NullTextScrubber。", nameof(userProfilePath));
            }

            string trimmed = userProfilePath.TrimEnd('\\', '/');
            _backslashed = trimmed.Replace('/', '\\');
            _slashed = trimmed.Replace('\\', '/');
        }

        public string Scrub(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // 同一个目录在日志里两种斜杠的写法都会出现，大小写也不一定一致。
            text = text.Replace(_backslashed, "~", StringComparison.OrdinalIgnoreCase);
            return text.Replace(_slashed, "~", StringComparison.OrdinalIgnoreCase);
        }
    }
}
