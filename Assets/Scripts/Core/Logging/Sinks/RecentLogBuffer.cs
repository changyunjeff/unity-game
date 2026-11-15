using System;

namespace Game.Core.Logging.Sinks
{
    /// <summary>
    /// 去处：内存里的最近记录。留最近若干行日志，另外单独留最近若干条错误，满了就盖掉最旧的。
    /// 崩溃包从这里取"出事前发生了什么"，以后的调试面板也从这里取。
    /// </summary>
    public sealed class RecentLogBuffer : ILogSink
    {
        private readonly object _gate = new object();
        private readonly Ring _lines;
        private readonly Ring _errors;

        public RecentLogBuffer(int lineCapacity = 500, int errorCapacity = 50)
        {
            _lines = new Ring(lineCapacity, nameof(lineCapacity));
            _errors = new Ring(errorCapacity, nameof(errorCapacity));
        }

        public void Write(in LogEntry entry)
        {
            lock (_gate)
            {
                _lines.Add(in entry);

                if (entry.Level == LogLevel.Error)
                {
                    _errors.Add(in entry);
                }
            }
        }

        /// <summary>最近的日志，从旧到新。</summary>
        public LogEntry[] Lines()
        {
            lock (_gate)
            {
                return _lines.ToArray();
            }
        }

        /// <summary>最近的错误，从旧到新。</summary>
        public LogEntry[] Errors()
        {
            lock (_gate)
            {
                return _errors.ToArray();
            }
        }

        // 定长的环形缓冲：写满之后，新的盖掉最旧的。
        private sealed class Ring
        {
            private readonly LogEntry[] _items;
            private int _written;

            public Ring(int capacity, string parameterName)
            {
                if (capacity <= 0)
                {
                    throw new ArgumentOutOfRangeException(parameterName);
                }

                _items = new LogEntry[capacity];
            }

            public void Add(in LogEntry entry)
            {
                _items[_written % _items.Length] = entry;
                _written++;
            }

            public LogEntry[] ToArray()
            {
                int count = Math.Min(_written, _items.Length);
                var result = new LogEntry[count];
                int start = _written - count;

                for (int i = 0; i < count; i++)
                {
                    result[i] = _items[(start + i) % _items.Length];
                }

                return result;
            }
        }
    }
}
