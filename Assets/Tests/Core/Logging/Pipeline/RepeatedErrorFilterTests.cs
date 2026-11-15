using System.Collections.Generic;
using Game.Core.Logging;
using Game.Core.Logging.Pipeline;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Pipeline
{
    public class RepeatedErrorFilterTests
    {
        // 把同一行送进去若干次，返回放过去的那些的内容。
        private static List<string> Passed(RepeatedErrorFilter filter, LogEntry entry, int times)
        {
            var passed = new List<string>();

            for (int i = 0; i < times; i++)
            {
                LogEntry current = entry;
                if (filter.Process(ref current))
                {
                    passed.Add(current.Message);
                }
            }

            return passed;
        }

        [Test]
        public void 同一个错刷屏_只放过第1_10_100次_并注明重复了多少次()
        {
            var filter = new RepeatedErrorFilter();

            List<string> passed = Passed(filter, TestEntries.Make(LogLevel.Error, "坏了"), 150);

            Assert.That(passed, Is.EqualTo(new[] { "坏了", "（同一个错已重复 10 次）坏了", "（同一个错已重复 100 次）坏了" }));
        }

        [Test]
        public void 内容_分类_调用栈有一样不同_就各算各的()
        {
            var filter = new RepeatedErrorFilter();

            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "甲"), 2), Has.Count.EqualTo(1));
            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "乙"), 1), Has.Count.EqualTo(1));
            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "甲", category: "Other"), 1), Has.Count.EqualTo(1));
            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "甲", "at A.B()"), 1), Has.Count.EqualTo(1));
        }

        [Test]
        public void 不是错误的_重复多少次都原样放过()
        {
            var filter = new RepeatedErrorFilter();

            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Warning, "慢了"), 20), Has.Count.EqualTo(20));
            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Info, "好了"), 20), Has.Count.EqualTo(20));
        }

        [Test]
        public void 不同的错超过记得住的种数之后_新的错每次都放过()
        {
            var filter = new RepeatedErrorFilter(maxKinds: 1);
            Passed(filter, TestEntries.Make(LogLevel.Error, "第一种"), 1);

            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "第二种"), 5), Has.Count.EqualTo(5));
            Assert.That(Passed(filter, TestEntries.Make(LogLevel.Error, "第一种"), 5), Is.Empty, "记住了的那一种照常去重");
        }
    }
}
