using System;
using Game.Core.Logging;
using NUnit.Framework;

namespace Game.Core.Tests.Logging
{
    /// <summary>入口门面：级别闸门，以及把内容包成一行交给输出。</summary>
    public class LogTests : CoreTestBase
    {
        [Test]
        public void 一行日志带着级别_分类名和内容交给输出()
        {
            Log.Info(new LogCategory("Save"), "读档完成");

            Assert.That(Logs.Entries, Has.Count.EqualTo(1));
            Assert.That(Logs.Entries[0].Level, Is.EqualTo(LogLevel.Info));
            Assert.That(Logs.Entries[0].Category, Is.EqualTo("Save"));
            Assert.That(Logs.Entries[0].Message, Is.EqualTo("读档完成"));
            Assert.That(Logs.Entries[0].Source, Is.EqualTo(LogSource.Game));
            Assert.That(Logs.Entries[0].StackTrace, Is.Null);
        }

        [Test]
        public void 分类默认放行到Info_更啰嗦的不输出()
        {
            var category = new LogCategory("Save");
            Logs.ExpectError("e");

            Log.Error(category, "e");
            Log.Warning(category, "w");
            Log.Info(category, "i");
            Log.Verbose(category, "v");
            Log.VeryVerbose(category, "vv");

            Assert.That(Logs.Messages, Is.EqualTo(new[] { "e", "w", "i" }));
        }

        [Test]
        public void 调高一个分类的级别_只有它变啰嗦()
        {
            var loud = new LogCategory("Loud") { MaxLevel = LogLevel.VeryVerbose };
            var quiet = new LogCategory("Quiet");

            Log.Verbose(loud, "v");
            Log.VeryVerbose(loud, "vv");
            Log.Verbose(quiet, "v");

            Assert.That(Logs.Entries, Has.Count.EqualTo(2));
            Assert.That(Logs.Entries[0].Category, Is.EqualTo("Loud"));
            Assert.That(Logs.Entries[1].Level, Is.EqualTo(LogLevel.VeryVerbose));
        }

        [Test]
        public void 调低一个分类的级别_Info被拦住_Error照出()
        {
            var category = new LogCategory("Save") { MaxLevel = LogLevel.Warning };
            Logs.ExpectError("e");

            Log.Info(category, "i");
            Log.Error(category, "e");

            Assert.That(Logs.Messages, Is.EqualTo(new[] { "e" }));
        }

        [Test]
        public void 分类必须有名字()
        {
            Assert.Throws<ArgumentException>(() => new LogCategory(""));
        }

        [Test]
        public void 接住的异常记成一条错误_带着异常的文字和它自己的调用栈()
        {
            Logs.ExpectError("写存档");
            Exception caught = null;
            try
            {
                throw new InvalidOperationException("磁盘满了");
            }
            catch (InvalidOperationException exception)
            {
                caught = exception;
            }

            Log.Exception(new LogCategory("Save"), caught, "写存档");

            Assert.That(Logs.Entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(Logs.Entries[0].Message, Is.EqualTo("写存档：磁盘满了"));
            Assert.That(Logs.Entries[0].StackTrace, Does.Contain("InvalidOperationException"));
            Assert.That(Logs.Entries[0].IsException, Is.False, "接住了的异常不算没人接住的异常");
        }

        [Test]
        public void 外面送进来的日志标明来源_带着调用栈和是不是没人接住的异常()
        {
            Logs.ExpectError("NullReferenceException");

            Log.WriteExternal(LogLevel.Error, new LogCategory("Unity"), "NullReferenceException", "at Foo.Bar()", true);

            Assert.That(Logs.Entries[0].Source, Is.EqualTo(LogSource.Unity));
            Assert.That(Logs.Entries[0].IsException, Is.True);
            Assert.That(Logs.Entries[0].StackTrace, Is.EqualTo("at Foo.Bar()"));
        }

        [Test]
        public void 输出不能设成null_要关掉就用空的那个()
        {
            Assert.Throws<ArgumentNullException>(() => Log.Output = null);

            Log.Output = NullLogSink.Instance;
            Assert.DoesNotThrow(() => Log.Info(new LogCategory("Save"), "没人收"));
            Assert.That(Logs.Entries, Is.Empty);
        }
    }
}
