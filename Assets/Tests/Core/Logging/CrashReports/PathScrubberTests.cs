using System;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class PathScrubberTests
    {
        [Test]
        public void 用户目录换成波浪号_两种斜杠和大小写都认()
        {
            var scrubber = new PathScrubber(@"C:\Users\zhangsan\");

            Assert.That(scrubber.Scrub(@"C:\Users\zhangsan\AppData\a.txt"), Is.EqualTo(@"~\AppData\a.txt"));
            Assert.That(scrubber.Scrub("C:/Users/zhangsan/AppData/a.txt"), Is.EqualTo("~/AppData/a.txt"));
            Assert.That(scrubber.Scrub(@"c:\users\ZHANGSAN\x 和 C:/users/zhangsan/y"), Is.EqualTo(@"~\x 和 ~/y"));
        }

        [Test]
        public void 别的内容原样不动()
        {
            var scrubber = new PathScrubber(@"C:\Users\zhangsan");

            Assert.That(scrubber.Scrub(@"D:\Games\a.txt"), Is.EqualTo(@"D:\Games\a.txt"));
            Assert.That(scrubber.Scrub(""), Is.EqualTo(""));
            Assert.That(scrubber.Scrub(null), Is.Null);
        }

        [Test]
        public void 用户目录不能为空_不需要处理时用空的那个()
        {
            Assert.Throws<ArgumentException>(() => new PathScrubber(null));
            Assert.That(NullTextScrubber.Instance.Scrub(@"C:\Users\zhangsan\a.txt"), Is.EqualTo(@"C:\Users\zhangsan\a.txt"));
        }
    }
}
