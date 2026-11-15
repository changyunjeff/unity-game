using System;
using Game.Core.Logging;

namespace Game.Core.Tests.Logging
{
    /// <summary>测试里造一行日志用的。</summary>
    public static class TestEntries
    {
        public static readonly LogStamp Stamp = new LogStamp(new DateTime(2026, 11, 15, 12, 3, 45, 120), 1234);

        public static LogEntry Make(LogLevel level, string message, string stackTrace = null, string category = "Save")
        {
            return new LogEntry(Stamp, level, category, message, stackTrace, LogSource.Game, false);
        }

        public static LogEntry Exception(string message)
        {
            return new LogEntry(Stamp, LogLevel.Error, "Unity", message, "at Foo.Bar()", LogSource.Unity, true);
        }
    }
}
