using System;
using System.Threading.Tasks;
using Game.Core.Logging;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 兜住主线程以外没人接住的异常，送进 <see cref="Log"/>，分类记为 Unhandled。
    /// （主线程上脚本抛的异常由 Unity 自己接住并报到 Console，那一路归 <see cref="UnityConsoleBridge"/>。）
    /// 建出来就开始听，<see cref="Dispose"/> 之后不再听。
    /// </summary>
    internal sealed class UnhandledExceptionCapture : IDisposable
    {
        private static readonly LogCategory LogUnhandled = new LogCategory("Unhandled");

        public UnhandledExceptionCapture()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }

        // 别的线程上抛出、一路没人接的异常。
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Report(args.ExceptionObject?.ToString() ?? "未知异常");
        }

        // 后台任务里抛了异常，却没有人去看它的结果。
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            Report(args.Exception.ToString());
        }

        // 异常的全文第一行当作内容，全文当作调用栈。
        private static void Report(string exceptionText)
        {
            int end = exceptionText.IndexOf('\n');
            string firstLine = end < 0 ? exceptionText : exceptionText.Substring(0, end).TrimEnd('\r');
            Log.WriteExternal(LogLevel.Error, LogUnhandled, firstLine, exceptionText, true);
        }
    }
}
