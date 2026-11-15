using System;

namespace Game.Core.Logging
{
    /// <summary>一行日志是谁打的。</summary>
    public enum LogSource : byte
    {
        /// <summary>我们自己的代码经 <see cref="Log"/> 打的。</summary>
        Game,

        /// <summary>Unity 自己或第三方代码报的，由引擎层的捕获送进来。</summary>
        Unity,
    }

    /// <summary>盖在一行日志上的戳：什么时间、第几帧。</summary>
    public readonly struct LogStamp
    {
        public readonly DateTime Time;
        public readonly int Frame;

        public LogStamp(DateTime time, int frame)
        {
            Time = time;
            Frame = frame;
        }
    }

    /// <summary>
    /// 一行日志的全部内容。它是不可变的：管线上的零件要改它，就用 <c>With…</c> 得到一份改过的新值。
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly LogStamp Stamp;
        public readonly LogLevel Level;
        public readonly string Category;
        public readonly string Message;

        /// <summary>调用栈。只有错误才带，别的是 null。</summary>
        public readonly string StackTrace;

        public readonly LogSource Source;

        /// <summary>是不是一个没人接住的异常。</summary>
        public readonly bool IsException;

        public LogEntry(LogStamp stamp, LogLevel level, string category, string message, string stackTrace, LogSource source, bool isException)
        {
            Stamp = stamp;
            Level = level;
            Category = category;
            Message = message;
            StackTrace = stackTrace;
            Source = source;
            IsException = isException;
        }

        public LogEntry WithStamp(LogStamp stamp) => new LogEntry(stamp, Level, Category, Message, StackTrace, Source, IsException);

        public LogEntry WithMessage(string message) => new LogEntry(Stamp, Level, Category, message, StackTrace, Source, IsException);

        public LogEntry WithStackTrace(string stackTrace) => new LogEntry(Stamp, Level, Category, Message, stackTrace, Source, IsException);
    }
}
