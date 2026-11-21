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

            Assert.That(queue.PendingUploads(), Is.EqualTo(new[] { older, newer }));
            Assert.That(queue.IsApproved(newer), Is.True);
        }

        [Test]
        public void 发出去了的包不再列出()
        {
            var queue = new CrashUploadQueue(_temp.Combine("CrashReports"));
            string sent = NewReport("20261120-120000-Exception");
            string waiting = NewReport("20261121-120000-Exception");
            queue.Approve(sent);
            queue.Approve(waiting);

            queue.MarkUploaded(sent);

            Assert.That(queue.IsUploaded(sent), Is.True);
            Assert.That(queue.PendingUploads(), Is.EqualTo(new[] { waiting }));
        }

        [Test]
        public void 同意的时候通知一声_带着包的目录()
        {
            var queue = new CrashUploadQueue(_temp.Combine("CrashReports"));
            string report = NewReport("20261121-120000-Exception");
            string heard = null;
            queue.ReportApproved += directory => heard = directory;

            queue.Approve(report);

            Assert.That(heard, Is.EqualTo(report));
        }

        [Test]
        public void 崩溃包目录还不存在_一个都没有()
        {
            Assert.That(new CrashUploadQueue(_temp.Combine("没有")).PendingUploads(), Is.Empty);
        }

        [Test]
        public void 同意或标记一个不存在的包_抛异常()
        {
            var queue = new CrashUploadQueue(_temp.Combine("CrashReports"));

            Assert.Throws<ArgumentException>(() => queue.Approve(_temp.Combine("没有")));
            Assert.Throws<ArgumentException>(() => queue.MarkUploaded(_temp.Combine("没有")));
            Assert.Throws<ArgumentException>(() => new CrashUploadQueue(""));
        }
    }
}
