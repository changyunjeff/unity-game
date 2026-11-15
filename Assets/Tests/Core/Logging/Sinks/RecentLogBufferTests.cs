using System;
using Game.Core.Logging;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class RecentLogBufferTests
    {
        private static string[] Messages(LogEntry[] entries) => Array.ConvertAll(entries, entry => entry.Message);

        [Test]
        public void 没满时_按写入的先后取出()
        {
            var buffer = new RecentLogBuffer(5, 5);

            buffer.Write(TestEntries.Make(LogLevel.Info, "1"));
            buffer.Write(TestEntries.Make(LogLevel.Info, "2"));

            Assert.That(Messages(buffer.Lines()), Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void 满了盖掉最旧的_取出来仍然从旧到新()
        {
            var buffer = new RecentLogBuffer(3, 3);

            for (int i = 1; i <= 5; i++)
            {
                buffer.Write(TestEntries.Make(LogLevel.Info, i.ToString()));
            }

            Assert.That(Messages(buffer.Lines()), Is.EqualTo(new[] { "3", "4", "5" }));
        }

        [Test]
        public void 错误另外单独留一份_不会被后面的普通日志挤掉()
        {
            var buffer = new RecentLogBuffer(2, 2);

            buffer.Write(TestEntries.Make(LogLevel.Error, "错一"));
            buffer.Write(TestEntries.Make(LogLevel.Warning, "警告不算错误"));
            buffer.Write(TestEntries.Make(LogLevel.Info, "a"));
            buffer.Write(TestEntries.Make(LogLevel.Info, "b"));

            Assert.That(Messages(buffer.Lines()), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Messages(buffer.Errors()), Is.EqualTo(new[] { "错一" }));
        }

        [Test]
        public void 容量必须是正数()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecentLogBuffer(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecentLogBuffer(5, 0));
        }
    }
}
