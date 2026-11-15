using System.Collections.Generic;

namespace Game.Core.Logging.Pipeline
{
    /// <summary>
    /// 工序：同一个错刷屏时只记一次。
    /// 分类、内容、调用栈都相同的错，第 1 次照过，之后只放过第 10、100、1000……次，并在内容前注明重复了多少次。
    /// 否则每帧抛一次的错一分钟就能写满日志文件。只管错误，别的级别原样放过。
    /// </summary>
    public sealed class RepeatedErrorFilter : ILogProcessor
    {
        private readonly Dictionary<string, int> _occurrences = new Dictionary<string, int>();
        private readonly int _maxKinds;

        /// <param name="maxKinds">最多记多少种不同的错。超出之后的新错不再计数，每次都照过。</param>
        public RepeatedErrorFilter(int maxKinds = 256)
        {
            _maxKinds = maxKinds;
        }

        public bool Process(ref LogEntry entry)
        {
            if (entry.Level != LogLevel.Error)
            {
                return true;
            }

            int occurrence = CountOccurrence(in entry);
            if (!IsPowerOfTen(occurrence))
            {
                return false;
            }

            if (occurrence > 1)
            {
                entry = entry.WithMessage("（同一个错已重复 " + occurrence + " 次）" + entry.Message);
            }

            return true;
        }

        private int CountOccurrence(in LogEntry entry)
        {
            string key = entry.Category + "\n" + entry.Message + "\n" + entry.StackTrace;

            lock (_occurrences)
            {
                if (_occurrences.TryGetValue(key, out int count))
                {
                    count++;
                    _occurrences[key] = count;
                    return count;
                }

                if (_occurrences.Count < _maxKinds)
                {
                    _occurrences.Add(key, 1);
                }

                return 1;
            }
        }

        // 1、10、100、1000……
        private static bool IsPowerOfTen(int value)
        {
            while (value >= 10 && value % 10 == 0)
            {
                value /= 10;
            }

            return value == 1;
        }
    }
}
