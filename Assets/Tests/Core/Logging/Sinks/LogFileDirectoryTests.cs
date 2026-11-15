using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Logging.Sinks;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.Sinks
{
    public class LogFileDirectoryTests
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 15, 21, 30, 5);

        private TempDirectory _temp;

        [SetUp]
        public void SetUp() => _temp = new TempDirectory();

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private string[] FileNames()
        {
            string[] names = Array.ConvertAll(Directory.GetFiles(_temp.Combine("Logs")), Path.GetFileName);
            Array.Sort(names, StringComparer.Ordinal);
            return names;
        }

        [Test]
        public void 文件名带着这次运行的开始时间_目录不存在会建()
        {
            var directory = new LogFileDirectory(_temp.Combine("Logs"));

            string path = directory.NewSessionFilePath(Start);

            Assert.That(Path.GetFileName(path), Is.EqualTo("log-20261115-213005.txt"));
            Assert.That(Directory.Exists(_temp.Combine("Logs")), Is.True);
            Assert.That(File.Exists(path), Is.False, "只起名字，不建文件");
        }

        [Test]
        public void 同一秒里起两次_后面的加序号()
        {
            var directory = new LogFileDirectory(_temp.Combine("Logs"));
            File.WriteAllText(directory.NewSessionFilePath(Start), "");

            Assert.That(Path.GetFileName(directory.NewSessionFilePath(Start)), Is.EqualTo("log-20261115-213005-2.txt"));
        }

        [Test]
        public void 只留最近几次_更早的删掉_别的文件不碰()
        {
            var directory = new LogFileDirectory(_temp.Combine("Logs"));
            for (int i = 0; i < 4; i++)
            {
                File.WriteAllText(directory.NewSessionFilePath(Start.AddMinutes(i)), "");
            }

            File.WriteAllText(Path.Combine(directory.Path, "notes.txt"), "");

            IReadOnlyList<string> failures = directory.DeleteOldSessions(2);

            Assert.That(failures, Is.Empty);
            Assert.That(FileNames(), Is.EqualTo(new[] { "log-20261115-213205.txt", "log-20261115-213305.txt", "notes.txt" }));
        }

        [Test]
        public void 旧文件被占着删不掉_不抛异常_把原因返回()
        {
            var directory = new LogFileDirectory(_temp.Combine("Logs"));
            string old = directory.NewSessionFilePath(Start);
            File.WriteAllText(old, "");
            File.WriteAllText(directory.NewSessionFilePath(Start.AddMinutes(1)), "");

            using (new FileStream(old, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                IReadOnlyList<string> failures = directory.DeleteOldSessions(1);

                Assert.That(failures, Has.Count.EqualTo(1));
                Assert.That(failures[0], Does.Contain("log-20261115-213005.txt"));
            }
        }

        [Test]
        public void 目录还不存在时清旧文件_什么都不做()
        {
            var directory = new LogFileDirectory(_temp.Combine("没有"));

            Assert.That(directory.DeleteOldSessions(5), Is.Empty);
        }

        [Test]
        public void 目录为空或保留次数不是正数_抛异常()
        {
            Assert.Throws<ArgumentException>(() => new LogFileDirectory(""));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogFileDirectory(_temp.Path).DeleteOldSessions(0));
        }
    }
}
