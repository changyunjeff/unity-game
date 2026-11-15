using System;
using System.Collections.Generic;
using Game.Core.Logging;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>
    /// 测试里接日志的去处：把收到的每一行记下来，并负责把关。
    /// 一条测试结束时，出现过没声明"这是预料中的"错误就判失败。用 <see cref="ExpectError"/> 声明。
    /// </summary>
    public sealed class LogWatch : ILogSink
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private readonly List<string> _expected = new List<string>();

        /// <summary>到现在收到的每一行日志，按先后。</summary>
        public IReadOnlyList<LogEntry> Entries => _entries;

        /// <summary>到现在收到的每一行的内容，按先后。</summary>
        public string[] Messages => _entries.ConvertAll(entry => entry.Message).ToArray();

        public void Write(in LogEntry entry) => _entries.Add(entry);

        /// <summary>声明：这条测试里会出现一条内容包含 <paramref name="containing"/> 的错误，这是预料中的。</summary>
        public void ExpectError(string containing) => _expected.Add(containing);

        /// <summary>每条错误都得是声明过的；每条声明也都得真的出现过。</summary>
        public void AssertNoUnexpectedErrors()
        {
            var seen = new bool[_expected.Count];

            foreach (LogEntry entry in _entries)
            {
                if (entry.Level != LogLevel.Error)
                {
                    continue;
                }

                int index = _expected.FindIndex(text => entry.Message.Contains(text, StringComparison.Ordinal));
                Assert.That(index, Is.GreaterThanOrEqualTo(0), "出现了没预料到的错误：[" + entry.Category + "] " + entry.Message);
                seen[index] = true;
            }

            for (int i = 0; i < seen.Length; i++)
            {
                Assert.That(seen[i], Is.True, "声明了会出现的错误没有出现：" + _expected[i]);
            }
        }
    }

    /// <summary>
    /// 核心层测试的基类。每条测试开始前把 <see cref="Log.Output"/> 换成 <see cref="LogWatch"/>，
    /// 结束时把关，再把原来的输出换回去。
    /// </summary>
    public abstract class CoreTestBase
    {
        private ILogSink _outputBefore;

        protected LogWatch Logs { get; private set; }

        [SetUp]
        public void InstallLogWatch()
        {
            _outputBefore = Log.Output;
            Logs = new LogWatch();
            Log.Output = Logs;
        }

        [TearDown]
        public void CheckLogWatch()
        {
            Log.Output = _outputBefore;
            Logs.AssertNoUnexpectedErrors();
        }
    }
}
