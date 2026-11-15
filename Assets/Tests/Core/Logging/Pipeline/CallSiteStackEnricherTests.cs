using Game.Core.Logging;
using Game.Core.Logging.Pipeline;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Pipeline
{
    public class CallSiteStackEnricherTests
    {
        // 经过"门面 → 管线 → 这道工序"打一条日志，返回到达输出的那一行。
        private static LogEntry ThroughPipeline(System.Action<LogCategory> log)
        {
            var output = new LogWatch();
            ILogSink before = Log.Output;
            Log.Output = new LogPipeline(NullLogClock.Instance, output, new CallSiteStackEnricher());

            try
            {
                log(new LogCategory("Save"));
            }
            finally
            {
                Log.Output = before;
            }

            return output.Entries[0];
        }

        [Test]
        public void 我们自己打的错误补上调用栈_从打日志的那一行开始_不含日志系统自己的那几层()
        {
            LogEntry entry = ThroughPipeline(category => Log.Error(category, "坏了"));

            string firstLine = entry.StackTrace.Split('\n')[0];
            Assert.That(firstLine, Does.Contain(nameof(CallSiteStackEnricherTests)));
            Assert.That(entry.StackTrace, Does.Not.Contain("Game.Core.Logging."));
        }

        [Test]
        public void 不是错误的不补()
        {
            LogEntry entry = ThroughPipeline(category => Log.Warning(category, "慢了"));

            Assert.That(entry.StackTrace, Is.Null);
        }

        [Test]
        public void 已经带着调用栈的不动()
        {
            var enricher = new CallSiteStackEnricher();
            LogEntry entry = TestEntries.Make(LogLevel.Error, "坏了", "at A.B()");

            Assert.That(enricher.Process(ref entry), Is.True);
            Assert.That(entry.StackTrace, Is.EqualTo("at A.B()"));
        }

        [Test]
        public void Unity送来的不补_它没有调用栈就是没有()
        {
            var enricher = new CallSiteStackEnricher();
            var entry = new LogEntry(default, LogLevel.Error, "Unity", "坏了", null, LogSource.Unity, false);

            enricher.Process(ref entry);

            Assert.That(entry.StackTrace, Is.Null);
        }
    }
}
