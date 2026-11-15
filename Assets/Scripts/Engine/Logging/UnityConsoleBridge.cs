using System;
using Game.Core.Logging;
using UnityEngine;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 我们的日志和 Unity 的 Console 之间的桥，两个方向都归它管：
    /// 出去：作为一个去处，把我们自己打的日志显示到 Console 里，每行开头带分类名。
    /// 进来：听 Unity 的日志回调，把 Unity 和第三方代码报的东西送进 <see cref="Log"/>，分类记为 Unity。
    ///
    /// 两个方向放在同一个类里，是因为它们之间有一个回声要掐掉：我们送出去的一行，Unity 的回调会原样送回来。
    /// 送出去的这段时间立个标记，回调看到标记就跳过。这个标记是它自己的私事，不暴露给别人。
    /// </summary>
    internal sealed class UnityConsoleBridge : ILogSink, IDisposable
    {
        private static readonly LogCategory LogUnity = new LogCategory("Unity");

        // Unity 的回调在"打日志的那个线程"上被调用，所以标记要每个线程各一份。
        [ThreadStatic]
        private static bool _sending;

        private bool _listening;

        // ---- 出去 ----

        public void Write(in LogEntry entry)
        {
            if (entry.Source == LogSource.Unity)
            {
                return; // 它本来就是从 Console 来的
            }

            string line = "[" + entry.Category + "] " + entry.Message;

            _sending = true;
            try
            {
                switch (entry.Level)
                {
                    case LogLevel.Error:
                        Debug.LogError(line);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(line);
                        break;
                    default:
                        Debug.Log(line);
                        break;
                }
            }
            finally
            {
                _sending = false;
            }
        }

        // ---- 进来 ----

        public void StartListening()
        {
            if (_listening)
            {
                return;
            }

            Application.logMessageReceivedThreaded += OnUnityLog;
            _listening = true;
        }

        public void Dispose()
        {
            if (!_listening)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= OnUnityLog;
            _listening = false;
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (_sending)
            {
                return; // 这是我们自己刚送出去的那一行的回声
            }

            switch (type)
            {
                case LogType.Exception:
                    Log.WriteExternal(LogLevel.Error, LogUnity, condition, stackTrace, true);
                    break;
                case LogType.Error:
                case LogType.Assert:
                    Log.WriteExternal(LogLevel.Error, LogUnity, condition, stackTrace, false);
                    break;
                case LogType.Warning:
                    Log.WriteExternal(LogLevel.Warning, LogUnity, condition, null, false);
                    break;
                default:
                    Log.WriteExternal(LogLevel.Info, LogUnity, condition, null, false);
                    break;
            }
        }
    }
}
