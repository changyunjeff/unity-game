using System;
using System.IO;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class CrashUploadQueueTests
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private string NewReport(string name)
        {
            string directory = Path.Combine(_temp.Combine("CrashReports"), name);
            Directory.CreateDirectory(directory);
            return directory;
        }

        [Test]
        public void 只列出同意过的包_旧的在前()
        {
            var queue = new CrashUploadQueue(_temp.Combine("CrashReports"));
            string newer = NewReport("20261121-120000-Exception");
            NewReport("20261120-120000-Exception");
            string older = NewReport("20261119-120000-UncleanExit");

            queue.Approve(newer);
            queue.Approve(older);

            Assert.That(queue.Approved(), Is.EqualTo(new[] { older, newer }));
            Assert.That(queue.IsApproved(newer), Is.True);
        }

        [Test]
        public void 崩溃包目录还不存在_一个都没有()
        {
            Assert.That(new CrashUploadQueue(_temp.Combine("没有")).Approved(), Is.Empty);
        }

        [Test]
        public void 同意一个不存在的包_抛异常()
        {
            var queue = new CrashUploadQueue(_temp.Combine("CrashReports"));

            Assert.Throws<ArgumentException>(() => queue.Approve(_temp.Combine("没有")));
            Assert.Throws<ArgumentException>(() => new CrashUploadQueue(""));
        }
    }
}
