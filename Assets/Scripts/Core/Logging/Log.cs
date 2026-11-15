using System;
using System.Diagnostics;

namespace Game.Core.Logging
{
    /// <summary>
    /// 打日志的入口。它只是一层薄薄的门面：检查分类的级别够不够，把内容包成一行，交给 <see cref="Output"/>。
    /// 时间、去重、调用栈、写到哪里，都是 <see cref="Output"/> 后面那条管线的事，不在这里。
    ///
    /// 三条规矩：
    /// 1. 遍历大量数据的循环体里不打日志。
    /// 2. 日志语句不能改游戏状态，不能消耗随机数。
    /// 3. Verbose 和 VeryVerbose 只在编辑器和开发版里存在；发布版里这两种调用连同参数
    ///    在编译时整句消失，所以可以放心写得详细。要在别处强制打开，定义 GAME_LOG_VERBOSE。
    /// </summary>
    public static class Log
    {
        private static ILogSink _output = NullLogSink.Instance;

        /// <summary>日志交给谁。启动时由装配点换成真正的管线；没换之前什么都不输出。</summary>
        public static ILogSink Output
        {
            get => _output;
            set => _output = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static void Error(LogCategory category, string message)
        {
            Write(LogLevel.Error, category, message, null, LogSource.Game, false);
        }

        /// <summary>在最外层接住了一个异常时用它记下来。级别是 Error，带着异常自己的调用栈。</summary>
        public static void Exception(LogCategory category, Exception exception, string message = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            string text = message == null ? exception.Message : message + "：" + exception.Message;
            Write(LogLevel.Error, category, text, exception.ToString(), LogSource.Game, false);
        }

        public static void Warning(LogCategory category, string message)
        {
            Write(LogLevel.Warning, category, message, null, LogSource.Game, false);
        }

        public static void Info(LogCategory category, string message)
        {
            Write(LogLevel.Info, category, message, null, LogSource.Game, false);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("GAME_LOG_VERBOSE")]
        public static void Verbose(LogCategory category, string message)
        {
            Write(LogLevel.Verbose, category, message, null, LogSource.Game, false);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("GAME_LOG_VERBOSE")]
        public static void VeryVerbose(LogCategory category, string message)
        {
            Write(LogLevel.VeryVerbose, category, message, null, LogSource.Game, false);
        }

        /// <summary>
        /// 把不是经 <see cref="Log"/> 打的东西送进来：Unity 自己报的日志、没人接住的异常。只给引擎层的捕获用。
        /// </summary>
        public static void WriteExternal(LogLevel level, LogCategory category, string message, string stackTrace, bool isException)
        {
            Write(level, category, message, stackTrace, LogSource.Unity, isException);
        }

        private static void Write(LogLevel level, LogCategory category, string message, string stackTrace, LogSource source, bool isException)
        {
            if (!category.IsEnabled(level))
            {
                return;
            }

            var entry = new LogEntry(default, level, category.Name, message, stackTrace, source, isException);
            _output.Write(in entry);
        }
    }
}
