namespace Game.Core.Logging.CrashReports
{
    /// <summary>崩溃包的几种原因。它们也是崩溃包目录名的最后一段。</summary>
    public static class CrashReasons
    {
        /// <summary>这次运行里第一次出现没人接住的异常。</summary>
        public const string Exception = "Exception";

        /// <summary>启动时发现上次运行没走到正常退出。</summary>
        public const string UncleanExit = "UncleanExit";

        /// <summary>开发时从菜单或代码里手动写的。</summary>
        public const string Manual = "Manual";
    }
}
