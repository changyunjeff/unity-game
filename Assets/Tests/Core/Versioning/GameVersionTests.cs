using System;
using Game.Core.Versioning;
using NUnit.Framework;

namespace Game.Core.Tests.Versioning
{
    public class GameVersionTests
    {
        [Test]
        public void 读出主次修三个数_写回去和原来一样()
        {
            GameVersion version = GameVersion.Parse("0.3.1");

            Assert.That(version.Major, Is.EqualTo(0));
            Assert.That(version.Minor, Is.EqualTo(3));
            Assert.That(version.Patch, Is.EqualTo(1));
            Assert.That(version.ToString(), Is.EqualTo("0.3.1"));
        }

        [Test]
        public void 前后的空白和换行不算()
        {
            Assert.That(GameVersion.Parse(" 1.20.300\r\n"), Is.EqualTo(new GameVersion(1, 20, 300)));
        }

        [TestCase("")]
        [TestCase("1.0")]
        [TestCase("1.0.0.0")]
        [TestCase("v1.0.0")]
        [TestCase("1.0.x")]
        [TestCase("1..0")]
        [TestCase("01.0.0")]
        [TestCase("1.0.-1")]
        [TestCase("1.0.0 beta")]
        [TestCase("1.0.99999999999")]
        public void 写法不对的读不出来(string text)
        {
            Assert.That(GameVersion.TryParse(text, out _), Is.False);
            Assert.Throws<FormatException>(() => GameVersion.Parse(text));
        }

        [Test]
        public void 没给文字也读不出来()
        {
            Assert.That(GameVersion.TryParse(null, out _), Is.False);
            Assert.Throws<FormatException>(() => GameVersion.Parse(null));
        }

        [Test]
        public void 三个数都相同才相等()
        {
            Assert.That(new GameVersion(0, 3, 1) == new GameVersion(0, 3, 1), Is.True);
            Assert.That(new GameVersion(0, 3, 1) != new GameVersion(0, 3, 2), Is.True);
            Assert.That(new GameVersion(0, 3, 1) != new GameVersion(1, 3, 1), Is.True);
        }

        [Test]
        public void 哪一位都不能是负数()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameVersion(0, 0, -1));
        }
    }
}
