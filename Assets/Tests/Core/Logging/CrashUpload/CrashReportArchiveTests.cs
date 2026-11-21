using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Game.Core.Logging.CrashReports;
using Game.Core.Logging.CrashUpload;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashUpload
{
    public class CrashReportArchiveTests
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        [Test]
        public void 包里的文件都压进去_内存转储和记号文件不放()
        {
            string report = _temp.Combine("20261121-120000-UncleanExit");
            Directory.CreateDirectory(Path.Combine(report, "Crash_1"));
            File.WriteAllText(Path.Combine(report, "info.txt"), "原因：上次运行没有正常退出\n");
            File.WriteAllText(Path.Combine(report, "Crash_1", "error.log"), "boom\n");
            File.WriteAllText(Path.Combine(report, "Crash_1", "crash.dmp"), "MDMP");
            File.WriteAllText(Path.Combine(report, CrashUploadQueue.ApprovedFileName), "");

            byte[] zip = CrashReportArchive.Zip(report);

            using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                string[] names = Array.ConvertAll(archive.Entries.ToArray(), entry => entry.FullName);
                Assert.That(names, Is.EquivalentTo(new[] { "Crash_1/error.log", "info.txt" }));

                using (var reader = new StreamReader(archive.GetEntry("info.txt").Open()))
                {
                    Assert.That(reader.ReadToEnd(), Is.EqualTo("原因：上次运行没有正常退出\n"));
                }
            }
        }

        [Test]
        public void 目录不存在_抛异常()
        {
            Assert.Throws<ArgumentException>(() => CrashReportArchive.Zip(_temp.Combine("没有")));
        }
    }
}
