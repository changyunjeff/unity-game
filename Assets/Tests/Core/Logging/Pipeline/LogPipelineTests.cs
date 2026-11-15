using System;
using System.Collections.Generic;
using Game.Core.Logging;
using Game.Core.Logging.Pipeline;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Pipeline
{
    public class LogPipelineTests
    {
        // 固定的时钟。
        private sealed class FixedClock : ILogClock
        {
            public LogStamp Stamp { get; set; }

            public LogStamp Now() => Stamp;
        }

        // 一道工序：记下自己被调用的先后，在内容后面加一个记号；可以设成拦下。
        private sealed class MarkingProcessor : ILogProcessor
        {
            private readonly string _mark;
            private readonly List<string> _order;

            public MarkingProcessor(string mark, List<string> order)
            {
                _mark = mark;
                _order = order;
            }

            public bool Drops { get; set; }

            public bool Process(ref LogEntry entry)
            {
                _order.Add(_mark);
                entry = entry.WithMessage(entry.Message + _mark);
                return !Drops;
            }
        }

        private static readonly LogEntry Unstamped = new LogEntry(default, LogLevel.Info, "Save", "读档", null, LogSource.Game, false);

        [Test]
        public void 一行日志先盖上时钟给的戳_再交给输出()
        {
            var clock = new FixedClock { Stamp = new LogStamp(new DateTime(2026, 11, 15, 12, 0, 0), 77) };
            var output = new LogWatch();
            var pipeline = new LogPipeline(clock, output);

            pipeline.Write(in Unstamped);

            Assert.That(output.Entries, Has.Count.EqualTo(1));
            Assert.That(output.Entries[0].Stamp.Frame, Is.EqualTo(77));
            Assert.That(output.Entries[0].Stamp.Time, Is.EqualTo(clock.Stamp.Time));
            Assert.That(output.Entries[0].Message, Is.EqualTo("读档"));
        }

        [Test]
        public void 工序按给的先后依次执行_后面的看到前面改过的内容()
        {
            var order = new List<string>();
            var output = new LogWatch();
            var pipeline = new LogPipeline(NullLogClock.Instance, output, new MarkingProcessor("甲", order), new MarkingProcessor("乙", order));

            pipeline.Write(in Unstamped);

            Assert.That(order, Is.EqualTo(new[] { "甲", "乙" }));
            Assert.That(output.Messages, Is.EqualTo(new[] { "读档甲乙" }));
        }

        [Test]
        public void 一道工序拦下之后_后面的工序和输出都不再收到()
        {
            var order = new List<string>();
            var output = new LogWatch();
            var pipeline = new LogPipeline(
                NullLogClock.Instance, output,
                new MarkingProcessor("甲", order) { Drops = true }, new MarkingProcessor("乙", order));

            pipeline.Write(in Unstamped);

            Assert.That(order, Is.EqualTo(new[] { "甲" }));
            Assert.That(output.Entries, Is.Empty);
        }

        [Test]
        public void 时钟和输出必须给_工序里不能有null()
        {
            var output = new LogWatch();

            Assert.Throws<ArgumentNullException>(() => new LogPipeline(null, output));
            Assert.Throws<ArgumentNullException>(() => new LogPipeline(NullLogClock.Instance, null));
            Assert.Throws<ArgumentException>(() => new LogPipeline(NullLogClock.Instance, output, new ILogProcessor[] { null }));
        }
    }
}
