namespace Game.Core.Logging.CrashReports
{
    /// <summary>隐私处理：文字写进崩溃包之前先过它一遍，把不该带走的东西换掉。</summary>
    public interface ITextScrubber
    {
        string Scrub(string text);
    }

    /// <summary>什么都不换。</summary>
    public sealed class NullTextScrubber : ITextScrubber
    {
        public static readonly NullTextScrubber Instance = new NullTextScrubber();

        private NullTextScrubber()
        {
        }

        public string Scrub(string text) => text;
    }
}
