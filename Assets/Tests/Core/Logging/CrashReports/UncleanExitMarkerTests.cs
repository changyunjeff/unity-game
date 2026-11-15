using System;
using System.IO;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class UncleanExitMarkerTests
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        [Test]
        public void 第一次运行_没有上次留下的标记()
        {
            var marker = new UncleanExitMarker(_temp.Combine("running.marker"));

            Assert.That(marker.TryReadLeftover(out string note), Is.False);
            Assert.That(note, Is.Null);
        }

        [Test]
        public void 上次放下标记后没拿走_这次启动读得到_连同留在里面的那句话()
        {
            string path = _temp.Combine("running.marker");
            new UncleanExitMarker(path).Set(@"D:\Logs\log-1.txt");

            var nextRun = new UncleanExitMarker(path);

            Assert.That(nextRun.TryReadLeftover(out string note), Is.True);
            Assert.That(note, Is.EqualTo(@"D:\Logs\log-1.txt"));
        }

        [Test]
        public void 正常退出时拿走了标记_下次启动就读不到()
        {
            string path = _temp.Combine("running.marker");
            var marker = new UncleanExitMarker(path);
            marker.Set("x");
            marker.Clear();

            Assert.That(new UncleanExitMarker(path).TryReadLeftover(out _), Is.False);
            Assert.DoesNotThrow(marker.Clear, "没有标记时再拿一次也不出错");
        }

        [Test]
        public void 标记所在的目录不存在会建_没给话就留空()
        {
            string path = Path.Combine(_temp.Combine("子目录"), "running.marker");
            var marker = new UncleanExitMarker(path);

            marker.Set(null);

            Assert.That(marker.TryReadLeftover(out string note), Is.True);
            Assert.That(note, Is.Empty);
        }

        [Test]
        public void 路径不能为空()
        {
            Assert.Throws<ArgumentException>(() => new UncleanExitMarker(""));
        }
    }
}
