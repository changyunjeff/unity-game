namespace Game.Core.Logging
{
    /// <summary>
    /// 日志级别。全项目统一含义，数值越大越啰嗦。
    /// </summary>
    public enum LogLevel
    {
        /// <summary>不该发生的事：断言没过、配置表有错、命令因"按规则不可能"的原因被拒。</summary>
        Error = 0,

        /// <summary>还能继续跑，但不正常：预算连续不够用、丢弃了一批日志。</summary>
        Warning = 1,

        /// <summary>生命周期：模块初始化、存档读档、顶层状态切换。分类的默认级别。</summary>
        Info = 2,

        /// <summary>细节：每条命令、某个对象的每次状态变化。发布版里编译时移除。</summary>
        Verbose = 3,

        /// <summary>最细：某个对象每一步的数值。发布版里编译时移除。</summary>
        VeryVerbose = 4,
    }
}
