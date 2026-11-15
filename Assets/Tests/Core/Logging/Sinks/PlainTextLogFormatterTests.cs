using Game.Core.Logging;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class PlainTextLogFormatterTests
    {
        private static string Format(LogEntry entry) => PlainTextLogFormatter.Instance.Format(in entry);

        [Test]
        public void 一行依次是时间_帧号_级别字母_分类_内容()
        {
            Assert.That(Format(TestEntries.Make(LogLevel.Error, "读档失败")), Is.EqualTo("12:03:45.120 F1234 E [Save] 读档失败"));
        }

        [Test]
        public void 调用栈每行缩进四格写在下面_空行去掉()
        {
            LogEntry entry = TestEntries.Make(LogLevel.Error, "坏了", "  at A.B()\r\n\r\n  at C.D()\n");

            Assert.That(Format(entry), Is.EqualTo("12:03:45.120 F1234 E [Save] 坏了\n    at A.B()\n    at C.D()"));
        }

        [TestCase(LogLevel.Error, "E")]
        [TestCase(LogLevel.Warning, "W")]
        [TestCase(LogLevel.Info, "I")]
        [TestCase(LogLevel.Verbose, "V")]
        [TestCase(LogLevel.VeryVerbose, "VV")]
        public void 五个级别各有各的字母(LogLevel level, string letter)
        {
            Assert.That(Format(TestEntries.Make(level, "x")), Is.EqualTo("12:03:45.120 F1234 " + letter + " [Save] x"));
        }
    }
}
