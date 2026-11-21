using System;
using System.IO;
using Game.Core.Logging.CrashReports;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashReports
{
    public class CrashPromptTests : CoreTestBase
    {
        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private string OptOutPath => _temp.Combine("crash-prompt-never-ask");

        private CrashUploadQueue Uploads => new CrashUploadQueue(_temp.Combine("CrashReports"));

        private CrashPrompt Prompt(bool enabled = true)
        {
            return new CrashPrompt(new CrashPromptOptOut(OptOutPath), Uploads, enabled);
        }

        private CrashReportWritten Report(string reason)
        {
            string directory = Path.Combine(_temp.Combine("CrashReports"), "20261121-120000-" + reason);
            Directory.CreateDirectory(directory);
            return new CrashReportWritten(reason, directory, new[] { "info.txt", "log.txt" });
        }

        [Test]
        public void 没人接住的异常和上次没正常退出_都要问玩家_界面取得到那个包()
        {
            CrashReportWritten report = Report(CrashReasons.UncleanExit);

            Assert.That(Prompt().Offer(report), Is.True);
            Assert.That(Prompt().Offer(Report(CrashReasons.Exception)), Is.True);

            CrashPrompt prompt = Prompt();
            prompt.Offer(report);

            Assert.That(prompt.TryGetPending(out CrashReportWritten pending), Is.True);
            Assert.That(pending.Directory, Is.EqualTo(report.Directory));
            Assert.That(pending.Files, Is.EqualTo(new[] { "info.txt", "log.txt" }));
        }

        [Test]
        public void 手动写的包不问()
        {
            CrashPrompt prompt = Prompt();

            Assert.That(prompt.Offer(Report(CrashReasons.Manual)), Is.False);
            Assert.That(prompt.TryGetPending(out _), Is.False);
        }

        [Test]
        public void 不弹的环境里什么都不问()
        {
            CrashPrompt prompt = Prompt(enabled: false);

            Assert.That(prompt.Offer(Report(CrashReasons.Exception)), Is.False);
            Assert.That(prompt.TryGetPending(out _), Is.False);
        }

        [Test]
        public void 一次运行最多问一次_答完了再来一个也不问()
        {
            CrashPrompt prompt = Prompt();
            prompt.Offer(Report(CrashReasons.UncleanExit));
            prompt.Answer(CrashPromptChoice.DontSend);

            Assert.That(prompt.Offer(Report(CrashReasons.Exception)), Is.False);
            Assert.That(prompt.TryGetPending(out _), Is.False);
            Assert.That(Logs.Messages, Has.Some.Contains("已经问过"));
        }

        [Test]
        public void 选发送_包记进待发送_界面收起()
        {
            CrashPrompt prompt = Prompt();
            CrashReportWritten report = Report(CrashReasons.Exception);
            prompt.Offer(report);

            prompt.Answer(CrashPromptChoice.Send);

            Assert.That(prompt.TryGetPending(out _), Is.False);
            Assert.That(Uploads.Approved(), Is.EqualTo(new[] { report.Directory }));
        }

        [Test]
        public void 选不发送_包不进待发送_下次运行照样会问()
        {
            CrashPrompt prompt = Prompt();
            prompt.Offer(Report(CrashReasons.Exception));

            prompt.Answer(CrashPromptChoice.DontSend);

            Assert.That(prompt.TryGetPending(out _), Is.False);
            Assert.That(Uploads.Approved(), Is.Empty);
            Assert.That(Prompt().Offer(Report(CrashReasons.UncleanExit)), Is.True);
        }

        [Test]
        public void 选以后都不问_包不进待发送_下次运行也不问()
        {
            CrashPrompt prompt = Prompt();
            prompt.Offer(Report(CrashReasons.Exception));

            prompt.Answer(CrashPromptChoice.NeverAsk);

            Assert.That(Uploads.Approved(), Is.Empty);
            Assert.That(new CrashPromptOptOut(OptOutPath).IsOptedOut, Is.True);
            Assert.That(Prompt().Offer(Report(CrashReasons.UncleanExit)), Is.False);
            Assert.That(Logs.Messages, Has.Some.Contains("以后都不问"));
        }

        [Test]
        public void 没有等着回答的包时回答_抛异常()
        {
            Assert.Throws<InvalidOperationException>(() => Prompt().Answer(CrashPromptChoice.Send));
        }

        [Test]
        public void 记号和待发送都必须给()
        {
            Assert.Throws<ArgumentNullException>(() => new CrashPrompt(null, Uploads, true));
            Assert.Throws<ArgumentNullException>(() => new CrashPrompt(new CrashPromptOptOut(OptOutPath), null, true));
        }
    }
}
