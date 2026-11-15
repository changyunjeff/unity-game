namespace Game.Core.Logging.Pipeline
{
    /// <summary>
    /// 管线上的一道工序。一行日志依次经过每道工序，每道只做一件事：
    /// 要么改它（补上调用栈、改写内容），要么拦下它。
    /// </summary>
    public interface ILogProcessor
    {
        /// <returns>false 表示这一行到此为止，不再往后走，也不会到达任何去处。</returns>
        bool Process(ref LogEntry entry);
    }
}
