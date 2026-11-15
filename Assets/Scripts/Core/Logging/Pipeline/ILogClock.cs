namespace Game.Core.Logging.Pipeline
{
    /// <summary>
    /// 时间和帧号从哪来。核心层自己不读时钟，所以由引擎层给一个实现；测试里给一个固定的。
    /// 可能在任何线程上被调用。
    /// </summary>
    public interface ILogClock
    {
        LogStamp Now();
    }

    /// <summary>没有时钟：戳是空的。</summary>
    public sealed class NullLogClock : ILogClock
    {
        public static readonly NullLogClock Instance = new NullLogClock();

        private NullLogClock()
        {
        }

        public LogStamp Now() => default;
    }
}
