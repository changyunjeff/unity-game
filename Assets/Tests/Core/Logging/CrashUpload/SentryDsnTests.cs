using System;
using Game.Core.Logging.CrashUpload;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashUpload
{
    public class SentryDsnTests
    {
        [Test]
        public void 从DSN里取出公钥_项目编号和收崩溃的地址()
        {
            SentryDsn dsn = SentryDsn.Parse("https://abc123@o42.ingest.sentry.io/456");

            Assert.That(dsn.PublicKey, Is.EqualTo("abc123"));
            Assert.That(dsn.ProjectId, Is.EqualTo("456"));
            Assert.That(dsn.EnvelopeUrl, Is.EqualTo("https://o42.ingest.sentry.io/api/456/envelope/"));
            Assert.That(dsn.AuthHeader("game/0.1.0"), Is.EqualTo("Sentry sentry_version=7, sentry_key=abc123, sentry_client=game/0.1.0"));
        }

        [Test]
        public void 自建的Sentry_端口和子路径都保留()
        {
            SentryDsn dsn = SentryDsn.Parse("http://key:secret@sentry.local:9000/base/7");

            Assert.That(dsn.PublicKey, Is.EqualTo("key"));
            Assert.That(dsn.EnvelopeUrl, Is.EqualTo("http://sentry.local:9000/base/api/7/envelope/"));
        }

        [Test]
        public void 写法不对_缺公钥或缺项目编号_抛异常()
        {
            Assert.Throws<ArgumentException>(() => SentryDsn.Parse(""));
            Assert.Throws<ArgumentException>(() => SentryDsn.Parse("不是网址"));
            Assert.Throws<ArgumentException>(() => SentryDsn.Parse("https://o42.ingest.sentry.io/456"));
            Assert.Throws<ArgumentException>(() => SentryDsn.Parse("https://abc@o42.ingest.sentry.io/"));
        }
    }
}
