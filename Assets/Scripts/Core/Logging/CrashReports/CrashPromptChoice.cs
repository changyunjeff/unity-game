namespace Game.Core.Logging.CrashReports
{
    /// <summary>玩家在崩溃界面上的选择。</summary>
    public enum CrashPromptChoice
    {
        /// <summary>同意把这个崩溃包发出去。</summary>
        Send,

        /// <summary>这次不发。</summary>
        DontSend,

        /// <summary>这次不发，以后也别再问。</summary>
        NeverAsk,
    }
}
