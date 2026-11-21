using System;
using System.IO;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class CrashPromptOptOutTests
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        [Test]
        public void 没选过就是没选过_选过之后换一个新的也读得到()
        {
            string path = Path.Combine(_temp.Combine("子目录"), "crash-prompt-never-ask");
            var optOut = new CrashPromptOptOut(path);

            Assert.That(optOut.IsOptedOut, Is.False);

            optOut.OptOut();

            Assert.That(new CrashPromptOptOut(path).IsOptedOut, Is.True);
        }

        [Test]
        public void 删掉记号文件就重新问()
        {
            string path = _temp.Combine("crash-prompt-never-ask");
            new CrashPromptOptOut(path).OptOut();

            File.Delete(path);

            Assert.That(new CrashPromptOptOut(path).IsOptedOut, Is.False);
        }

        [Test]
        public void 路径不能为空()
        {
            Assert.Throws<ArgumentException>(() => new CrashPromptOptOut(""));
        }
    }
}
