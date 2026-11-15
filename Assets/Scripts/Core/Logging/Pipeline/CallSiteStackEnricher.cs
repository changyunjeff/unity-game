using System;
using System.Diagnostics;

namespace Game.Core.Logging.Pipeline
{
    /// <summary>
    /// 工序：给我们自己打的错误补上调用栈，这样文件和崩溃包里能看到这一行是哪里打的。
    /// 调用栈从日志系统外面的第一层开始：日志系统自己的那几层（门面、管线、工序）全部略去。
    /// 已经带着调用栈的（接住的异常、Unity 送来的）不动。放在去重工序之后，被拦下的错就不用白算一次。
    /// </summary>
    public sealed class CallSiteStackEnricher : ILogProcessor
    {
        private static readonly string LoggingNamespace = typeof(Log).Namespace;

        public bool Process(ref LogEntry entry)
        {
            if (entry.Level == LogLevel.Error && entry.StackTrace == null && entry.Source == LogSource.Game)
            {
                entry = entry.WithStackTrace(CaptureOutsideLogging());
            }

            return true;
        }

        private static string CaptureOutsideLogging()
        {
            var whole = new StackTrace(false);
            int skip = 0;

            while (skip < whole.FrameCount && IsInsideLogging(whole.GetFrame(skip)))
            {
                skip++;
            }

            // 这一次和上面那一次是在同一个函数里取的，所以跳过的层数对得上。
            return new StackTrace(skip, true).ToString();
        }

        private static bool IsInsideLogging(StackFrame frame)
        {
            Type type = frame.GetMethod()?.DeclaringType;
            string typeNamespace = type?.Namespace;
            return typeNamespace != null && typeNamespace.StartsWith(LoggingNamespace, StringComparison.Ordinal);
        }
    }
}
