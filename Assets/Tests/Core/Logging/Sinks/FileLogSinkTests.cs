using System;
using System.IO;
using Game.Core.Logging;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class FileLogSinkTests
    {
        // 测试用的格式：只写内容，这样断言不用跟着真正的格式变。
        private sealed class MessageOnlyFormatter : ILogFormatter
        {
            public string Format(in LogEntry entry) => entry.Message;
        }

        private static readonly ILogFormatter Formatter = new MessageOnlyFormatter();

        private TempDirectory _temp;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempDirectory();
            _path = _temp.Combine("log.txt");
        }

        [TearDown]
        public void TearDown() => _temp.Dispose();

        [Test]
        public void 文件开头先写给的那几行()
        {
            using (var sink = new FileLogSink(_path, Formatter, new[] { "版本 0.1", "系统 Windows" }))
            {
                Assert.That(sink.FilePath, Is.EqualTo(_path));
                Assert.That(TempDirectory.ReadAll(_path), Is.EqualTo("版本 0.1\n系统 Windows\n"));
            }
        }

        [Test]
        public void 每一行按给的格式写_Info及以上写完立刻能在文件里读到()
        {
            using (var sink = new FileLogSink(_path, Formatter))
            {
                sink.Write(TestEntries.Make(LogLevel.Info, "读档完成"));
                sink.Write(TestEntries.Make(LogLevel.Error, "读档失败"));

                Assert.That(TempDirectory.ReadAll(_path), Is.EqualTo("读档完成\n读档失败\n"));
            }
        }

        [Test]
        public void Verbose攒着_Flush之后才落盘()
        {
            using (var sink = new FileLogSink(_path, Formatter))
            {
                sink.Write(TestEntries.Make(LogLevel.Verbose, "细节"));
                Assert.That(TempDirectory.ReadAll(_path), Is.Empty);

                sink.Flush();
                Assert.That(TempDirectory.ReadAll(_path), Is.EqualTo("细节\n"));
            }
        }

        [Test]
        public void 写到上限就停_末尾注明_之后的不再写()
        {
            using (var sink = new FileLogSink(_path, Formatter, null, maxBytes: 100))
            {
                sink.Write(TestEntries.Make(LogLevel.Info, new string('a', 200)));
                sink.Write(TestEntries.Make(LogLevel.Info, "这一行不该出现"));

                string text = TempDirectory.ReadAll(_path);
                Assert.That(text, Does.EndWith("日志文件已到上限，这次运行后面的日志不再写入。\n"));
                Assert.That(text, Does.Not.Contain("这一行不该出现"));
            }
        }

        [Test]
        public void 关掉之后再写_不出错也不写()
        {
            var sink = new FileLogSink(_path, Formatter);
            sink.Dispose();

            Assert.DoesNotThrow(() => sink.Write(TestEntries.Make(LogLevel.Error, "晚了")));
            Assert.DoesNotThrow(sink.Flush);
            Assert.DoesNotThrow(sink.Dispose);
            Assert.That(TempDirectory.ReadAll(_path), Is.Empty);
        }

        [Test]
        public void 文件已经存在_路径为空_没给格式_都抛异常()
        {
            File.WriteAllText(_path, "旧的");

            Assert.Throws<IOException>(() => new FileLogSink(_path, Formatter));
            Assert.Throws<ArgumentException>(() => new FileLogSink("", Formatter));
            Assert.Throws<ArgumentNullException>(() => new FileLogSink(_temp.Combine("另一个.txt"), null));
        }
    }
}
