namespace Game.Core.Logging
{
    /// <summary>
    /// 能接收一行日志的东西。整个日志系统只有这一个抽象：
    /// 管线、多个去处的组合、文件、内存缓冲、Unity 的 Console，都是它的实现，所以可以任意套接。
    /// 实现可能在任何线程上被调用。
    /// </summary>
    public interface ILogSink
    {
        void Write(in LogEntry entry);
    }

    /// <summary>什么都不做的去处。日志系统没装好之前，<see cref="Log"/> 的输出就是它。</summary>
    public sealed class NullLogSink : ILogSink
    {
        public static readonly NullLogSink Instance = new NullLogSink();

        private NullLogSink()
        {
        }

        public void Write(in LogEntry entry)
        {
        }
    }
}
