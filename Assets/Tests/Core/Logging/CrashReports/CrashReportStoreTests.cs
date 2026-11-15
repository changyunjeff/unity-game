using System;
using System.IO;
using Game.Core.Logging;
using Game.Core.Logging.CrashReports;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class CrashReportStoreTests
    {
        private static readonly DateTime Time = new DateTime(2026, 11, 15, 22, 0, 9);

        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private CrashReportStore Store(ITextScrubber scrubber = null, int keep = 10)
        {
            return new CrashReportStore(_temp.Combine("CrashReports"), scrubber ?? NullTextScrubber.Instance, PlainTextLogFormatter.Instance, keep);
        }

        private static string Read(string directory, string fileName) => File.ReadAllText(Path.Combine(directory, fileName));

        [Test]
        public void 一份报告存成一个目录_名字是时间加原因_里面有信息和出事前的日志()
        {
            var report = new CrashReport("Exception", Time)
            {
                RecentEntries = new[] { TestEntries.Make(LogLevel.Error, "读档失败") },
            };
            report.Details.Add("原因：异常");
            report.Details.Add("版本：0.1");

            string directory = Store().Save(report);

            Assert.That(Path.GetFileName(directory), Is.EqualTo("20261115-220009-Exception"));
            Assert.That(Read(directory, CrashReportStore.InfoFileName), Is.EqualTo("原因：异常\n版本：0.1\n"));
            Assert.That(Read(directory, CrashReportStore.LogFileName), Is.EqualTo("12:03:45.120 F1234 E [Save] 读档失败\n"));
        }

        [Test]
        public void 原因里不能当目录名的字符换成下划线_没给原因写Unknown()
        {
            CrashReportStore store = Store();

            string first = store.Save(new CrashReport("a b/c:退出_1-x", Time));
            string second = store.Save(new CrashReport(null, Time));

            Assert.That(Path.GetFileName(first), Is.EqualTo("20261115-220009-a_b_c____1-x"));
            Assert.That(Path.GetFileName(second), Is.EqualTo("20261115-220009-Unknown"));
        }

        [Test]
        public void 同一秒同一个原因存两次_目录不重名()
        {
            CrashReportStore store = Store();

            string first = store.Save(new CrashReport("Manual", Time));
            string second = store.Save(new CrashReport("Manual", Time));

            Assert.That(Path.GetFileName(second), Is.EqualTo(Path.GetFileName(first) + "-2"));
        }

        [Test]
        public void 写进去的每一行都做隐私处理_带走的文件也一样()
        {
            string playerLog = _temp.Combine("Player.log");
            File.WriteAllText(playerLog, "Loading C:/Users/zhangsan/AppData/game.dat\nok\n");

            var report = new CrashReport("Exception", Time)
            {
                RecentEntries = new[] { TestEntries.Make(LogLevel.Error, @"打不开 c:\users\ZhangSan\Saves\a.sav") },
            };
            report.Details.Add(@"存档：C:\Users\zhangsan\Saves\a.sav");
            report.Attachments.Add(playerLog);

            string directory = Store(new PathScrubber(@"C:\Users\zhangsan")).Save(report);

            Assert.That(Read(directory, CrashReportStore.InfoFileName), Is.EqualTo("存档：~\\Saves\\a.sav\n带上了：Player.log\n"));
            Assert.That(Read(directory, CrashReportStore.LogFileName), Does.Contain(@"打不开 ~\Saves\a.sav"));
            Assert.That(Read(directory, "Player.log"), Is.EqualTo("Loading ~/AppData/game.dat\nok\n"));
        }

        [Test]
        public void 要带走的文件不存在_在信息里注明_不出错()
        {
            var report = new CrashReport("Exception", Time);
            report.Attachments.Add(_temp.Combine("没有.log"));

            string directory = Store().Save(report);

            Assert.That(Read(directory, CrashReportStore.InfoFileName), Does.Contain("没有带上").And.Contain("文件不存在"));
        }

        [Test]
        public void 只留最近几个崩溃包()
        {
            CrashReportStore store = Store(keep: 2);

            for (int i = 0; i < 4; i++)
            {
                store.Save(new CrashReport("Manual", Time.AddMinutes(i)));
            }

            string[] names = Array.ConvertAll(Directory.GetDirectories(store.RootDirectory), Path.GetFileName);
            Array.Sort(names, StringComparer.Ordinal);

            Assert.That(names, Is.EqualTo(new[] { "20261115-220209-Manual", "20261115-220309-Manual" }));
        }

        [Test]
        public void 目录_隐私处理_格式都必须给_保留个数必须是正数()
        {
            Assert.Throws<ArgumentException>(() => new CrashReportStore("", NullTextScrubber.Instance, PlainTextLogFormatter.Instance));
            Assert.Throws<ArgumentNullException>(() => new CrashReportStore(_temp.Path, null, PlainTextLogFormatter.Instance));
            Assert.Throws<ArgumentNullException>(() => new CrashReportStore(_temp.Path, NullTextScrubber.Instance, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrashReportStore(_temp.Path, NullTextScrubber.Instance, PlainTextLogFormatter.Instance, 0));
            Assert.Throws<ArgumentNullException>(() => Store().Save(null));
        }
    }
}
