using System;
using System.Threading;

namespace Game.Core.Logging.Sinks
{
    /// <summary>
    /// 去处：盯着"没人接住的异常"。第一次看到时通知一声，之后的不再通知。
    /// 用来触发"一次运行只写一个"的崩溃包。它自己不知道崩溃包是什么，只管通知。
    /// </summary>
    public sealed class FirstExceptionSink : ILogSink
    {
        private readonly Action<LogEntry> _onFirstException;
        private int _fired;

        public FirstExceptionSink(Action<LogEntry> onFirstException)
        {
            _onFirstException = onFirstException ?? throw new ArgumentNullException(nameof(onFirstException));
        }

        public void Write(in LogEntry entry)
        {
            if (!entry.IsException)
            {
                return;
            }

            // 异常可能同时在几个线程上冒出来，只让抢到的那一个去通知。
            if (Interlocked.Exchange(ref _fired, 1) == 0)
            {
                _onFirstException(entry);
            }
        }
    }
}
