using System;
using Game.Core.Flow;
using Game.Core.Logging;
using Game.Engine.Logging;
using UnityEngine;

namespace Game.Engine
{
    /// <summary>
    /// 整个程序唯一的装配点：贯穿运行始终的系统，全在这里按先后建起来，退出时按相反的先后收掉。
    /// 别的代码要用这些系统，从这里拿。新增一个贯穿始终的系统，就在 <see cref="Start"/> 里加一行、
    /// 在 <see cref="Stop"/> 里对应加一行，不另外去挂 Unity 的启动回调。
    /// </summary>
    public static class GameRuntime
    {
        private static readonly LogCategory LogApp = new LogCategory("App");

        /// <summary>日志系统。没在运行时是 null。</summary>
        public static LogSystem Logging { get; private set; }

        /// <summary>程序运行状态。没在运行时是 null。</summary>
        public static AppFlow Flow { get; private set; }

        // Unity 每次开始运行时最早的一个时机：在任何场景和脚本之前。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Start()
        {
            // 编辑器里关了"进入运行时重载代码"的话，上一次运行建的东西还在，先收掉。
            Stop();

            Logging = LogSystem.Start(LogSystemSettings.FromUnity(), SessionInfo.Collect(DateTime.Now));
            Flow = new AppFlow();

            Application.quitting += Stop;
            Log.Info(LogApp, "启动完成，当前 " + Flow.State);
        }

        // 第一个场景加载之前：这时才能改 Unity 每帧要跑的步骤。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeFirstScene() => UnityLogClock.TrackFrames();

        // 程序退出（编辑器里是停止运行）时由 Unity 调用。先后和 Start 相反：日志系统最后收，这样前面每一步都留得下日志。
        private static void Stop()
        {
            Application.quitting -= Stop;

            if (Flow != null)
            {
                if (Flow.State != AppState.Shutdown)
                {
                    Flow.Request(AppState.Shutdown);
                }

                Flow = null;
            }

            Logging?.Dispose();
            Logging = null;
        }
    }
}
