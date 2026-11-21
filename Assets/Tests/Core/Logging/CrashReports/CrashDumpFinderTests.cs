using System;
using System.IO;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class CrashDumpFinderTests
    {
        private static readonly DateTime PreviousStart = new DateTime(2026, 11, 21, 12, 0, 0);

        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private string Crash(string name, DateTime time)
        {
            string directory = Path.Combine(_temp.Combine("Crashes"), name);
            Directory.CreateDirectory(directory);
            Directory.SetLastWriteTime(directory, time);
            return directory;
        }

        [Test]
        public void 只认那个时刻当时或之后留下的目录_旧的在前()
        {
            Crash("Crash_2026-11-20_090000000", PreviousStart.AddDays(-1));
            string atStart = Crash("Crash_2026-11-21_120000000", PreviousStart);
            string later = Crash("Crash_2026-11-21_123000000", PreviousStart.AddMinutes(30));

            var finder = new CrashDumpFinder(_temp.Combine("Crashes"));

            Assert.That(finder.FoldersSince(PreviousStart), Is.EqualTo(new[] { atStart, later }));
        }

        [Test]
        public void 目录不存在或没给目录_一个都没有()
        {
            Assert.That(new CrashDumpFinder(_temp.Combine("没有")).FoldersSince(PreviousStart), Is.Empty);
            Assert.That(new CrashDumpFinder(null).FoldersSince(PreviousStart), Is.Empty);
        }
    }
}
