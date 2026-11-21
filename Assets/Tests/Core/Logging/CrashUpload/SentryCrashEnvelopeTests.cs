using System;
using System.IO;
using System.Text;
using Game.Core.Logging.CrashUpload;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashUpload
{
    public class SentryCrashEnvelopeTests
    {
        private static readonly Guid EventId = new Guid("0123456789abcdef0123456789abcdef");
        private static readonly DateTime Time = new DateTime(2026, 11, 21, 4, 30, 0, DateTimeKind.Utc);
        private static readonly SentryDsn Dsn = SentryDsn.Parse("https://abc@o42.ingest.sentry.io/456");

        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private string Report(string name, string info)
        {
            string report = _temp.Combine(name);
            Directory.CreateDirectory(report);
            File.WriteAllText(Path.Combine(report, "info.txt"), info);
            return report;
        }

        // envelope 按行拆开：第一行是头，之后每个条目是一行头加一段内容。内容里的字节在这几个测试里都是可读的文字或 zip。
        private static string[] HeaderLines(byte[] envelope)
        {
            string text = Encoding.UTF8.GetString(envelope);
            return text.Split('\n');
        }

        [Test]
        public void 头里有事件编号和DSN_第一个条目是事件_带着原因_版本和info全文()
        {
            string report = Report("20261121-123000-Exception", "原因：Exception\n异常：读档失败\n");
            var envelope = new SentryCrashEnvelope(Dsn, "0.1.0", "production");

            string[] lines = HeaderLines(envelope.Build(report, EventId, Time));

            Assert.That(lines[0], Is.EqualTo("{\"event_id\":\"0123456789abcdef0123456789abcdef\",\"dsn\":\"https://abc@o42.ingest.sentry.io/456\"}"));
            Assert.That(lines[1], Does.StartWith("{\"type\":\"event\",\"length\":"));
            Assert.That(lines[2], Does.Contain("\"timestamp\":\"2026-11-21T04:30:00Z\"")
                .And.Contain("\"platform\":\"other\"")
                .And.Contain("\"release\":\"0.1.0\"")
                .And.Contain("\"environment\":\"production\"")
                .And.Contain("\"message\":{\"formatted\":\"原因：Exception\"}")
                .And.Contain("\"tags\":{\"reason\":\"Exception\"}")
                .And.Contain("\"info\":\"原因：Exception\\n异常：读档失败\\n\""));
            Assert.That(lines[3], Does.StartWith("{\"type\":\"attachment\"").And.Contain("\"filename\":\"20261121-123000-Exception.zip\""));
        }

        [Test]
        public void 条目头里写的长度就是内容的字节数()
        {
            string report = Report("20261121-123000-Exception", "原因：中文占三个字节\n");

            byte[] envelope = new SentryCrashEnvelope(Dsn, "0.1.0", "production").Build(report, EventId, Time);
            string[] lines = HeaderLines(envelope);

            int length = int.Parse(lines[1].Split(new[] { "\"length\":" }, StringSplitOptions.None)[1].TrimEnd('}'));
            Assert.That(Encoding.UTF8.GetByteCount(lines[2]), Is.EqualTo(length));
        }

        [Test]
        public void 有内存转储时_多一个minidump附件_事件算native_只发最新的一个()
        {
            string report = Report("20261121-123000-UncleanExit", "原因：上次运行没有正常退出\n");
            Directory.CreateDirectory(Path.Combine(report, "Crash_2026-11-21_100000000"));
            Directory.CreateDirectory(Path.Combine(report, "Crash_2026-11-21_110000000"));
            File.WriteAllText(Path.Combine(report, "Crash_2026-11-21_100000000", "crash.dmp"), "OLD");
            File.WriteAllText(Path.Combine(report, "Crash_2026-11-21_110000000", "crash.dmp"), "NEW");

            string text = Encoding.UTF8.GetString(new SentryCrashEnvelope(Dsn, "0.1.0", "production").Build(report, EventId, Time));

            Assert.That(text, Does.Contain("\"platform\":\"native\""));
            Assert.That(text, Does.Contain("{\"type\":\"attachment\",\"length\":3,\"filename\":\"crash.dmp\",\"attachment_type\":\"event.minidump\"}\nNEW\n"));
            Assert.That(text, Does.Not.Contain("OLD\n"));
        }

        [Test]
        public void 从目录名取原因_带序号的也认()
        {
            Assert.That(SentryCrashEnvelope.ReasonOf("20261121-123000-Exception"), Is.EqualTo("Exception"));
            Assert.That(SentryCrashEnvelope.ReasonOf("20261121-123000-Manual-2"), Is.EqualTo("Manual"));
            Assert.That(SentryCrashEnvelope.ReasonOf("乱写的"), Is.EqualTo("Unknown"));
        }

        [Test]
        public void 目录不存在_抛异常()
        {
            var envelope = new SentryCrashEnvelope(Dsn, "0.1.0", "production");

            Assert.Throws<ArgumentException>(() => envelope.Build(_temp.Combine("没有"), EventId, Time));
            Assert.Throws<ArgumentNullException>(() => new SentryCrashEnvelope(null, "0.1.0", "production"));
        }
    }
}
