using System;
using Game.Core.Logging;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class CompositeLogSinkTests
    {
        // 一写就抛异常的去处。
        private sealed class BrokenSink : ILogSink
        {
            public int Calls { get; private set; }

            public void Write(in LogEntry entry)
            {
                Calls++;
                throw new InvalidOperationException("磁盘坏了");
            }
        }

        [Test]
        public void 收到一行_里面的每个去处都收到()
        {
            var first = new LogWatch();
            var second = new LogWatch();
            var composite = new CompositeLogSink(first, second);

            composite.Write(TestEntries.Make(LogLevel.Info, "读档"));

            Assert.That(composite.Count, Is.EqualTo(2));
            Assert.That(first.Messages, Is.EqualTo(new[] { "读档" }));
            Assert.That(second.Messages, Is.EqualTo(new[] { "读档" }));
        }

        [Test]
        public void 运行中加进来的去处从加进来之后开始收到_拿掉的不再收到()
        {
            var first = new LogWatch();
            var late = new LogWatch();
            var composite = new CompositeLogSink(first);

            composite.Write(TestEntries.Make(LogLevel.Info, "1"));
            composite.Add(late);
            composite.Write(TestEntries.Make(LogLevel.Info, "2"));

            Assert.That(composite.Remove(first), Is.True);
            Assert.That(composite.Remove(first), Is.False, "已经不在了");
            composite.Write(TestEntries.Make(LogLevel.Info, "3"));

            Assert.That(first.Messages, Is.EqualTo(new[] { "1", "2" }));
            Assert.That(late.Messages, Is.EqualTo(new[] { "2", "3" }));
        }

        [Test]
        public void 同一个去处加两次_或者加null_抛异常()
        {
            var sink = new LogWatch();
            var composite = new CompositeLogSink(sink);

            Assert.Throws<InvalidOperationException>(() => composite.Add(sink));
            Assert.Throws<ArgumentNullException>(() => composite.Add(null));
        }

        [Test]
        public void 一个去处写坏了_它被拿掉_别的去处照常收到并多一条错误_写日志的人不受连累()
        {
            var healthy = new LogWatch();
            var broken = new BrokenSink();
            var composite = new CompositeLogSink(broken, healthy);

            Assert.DoesNotThrow(() => composite.Write(TestEntries.Make(LogLevel.Info, "第一行")));
            composite.Write(TestEntries.Make(LogLevel.Info, "第二行"));

            Assert.That(broken.Calls, Is.EqualTo(1), "拿掉之后不该再交给它");
            Assert.That(composite.Count, Is.EqualTo(1));
            Assert.That(healthy.Entries, Has.Count.EqualTo(3));
            Assert.That(healthy.Entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(healthy.Entries[0].Message, Does.Contain("BrokenSink").And.Contain("磁盘坏了"));
            Assert.That(healthy.Entries[0].Stamp.Frame, Is.EqualTo(TestEntries.Stamp.Frame), "报告沿用出事那一行的戳");
            Assert.That(healthy.Messages[1], Is.EqualTo("第一行"));
            Assert.That(healthy.Messages[2], Is.EqualTo("第二行"));
        }

        [Test]
        public void 里面一个去处都没有_写了也不出错()
        {
            var composite = new CompositeLogSink();

            Assert.DoesNotThrow(() => composite.Write(TestEntries.Make(LogLevel.Error, "没人收")));
        }
    }
}
