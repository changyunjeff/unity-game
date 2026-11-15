using System;
using System.Collections.Generic;
using Game.Core.Logging;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class FirstExceptionSinkTests
    {
        [Test]
        public void 第一次看到没人接住的异常时通知一声_之后的不再通知()
        {
            var notified = new List<string>();
            var sink = new FirstExceptionSink(entry => notified.Add(entry.Message));

            sink.Write(TestEntries.Exception("第一个"));
            sink.Write(TestEntries.Exception("第二个"));

            Assert.That(notified, Is.EqualTo(new[] { "第一个" }));
        }

        [Test]
        public void 普通的错误和别的日志不通知()
        {
            int notified = 0;
            var sink = new FirstExceptionSink(entry => notified++);

            sink.Write(TestEntries.Make(LogLevel.Error, "只是错误"));
            sink.Write(TestEntries.Make(LogLevel.Info, "没事"));

            Assert.That(notified, Is.Zero);
        }

        [Test]
        public void 必须给通知的对象()
        {
            Assert.Throws<ArgumentNullException>(() => new FirstExceptionSink(null));
        }
    }
}
