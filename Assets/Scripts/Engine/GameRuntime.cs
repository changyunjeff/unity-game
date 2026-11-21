using System;
using Game.Core.Flow;
using Game.Core.Logging;
using Game.Core.Logging.CrashReports;
using Game.Core.Logging.CrashUpload;
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

        /// <summary>崩溃界面要不要弹、玩家怎么答的。没在运行时是 null。</summary>
        public static CrashPrompt CrashPrompt { get; private set; }

        private static CrashPromptView _crashPromptView;
        private static SentryCrashUploader _crashUploader;

        // Unity 每次开始运行时最早的一个时机：在任何场景和脚本之前。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Start()
        {
            // 编辑器里关了"进入运行时重载代码"的话，上一次运行建的东西还在，先收掉。
            Stop();

            LogSystemSettings settings = LogSystemSettings.FromUnity();

            // 崩溃界面的决定要在日志系统之前建好：日志系统一起来就可能补写"上次没正常退出"的包，要能马上交给它。
            var uploads = new CrashUploadQueue(settings.CrashReportDirectory);
            CrashPrompt = new CrashPrompt(new CrashPromptOptOut(settings.CrashPromptOptOutPath), uploads, settings.PromptOnCrash);
            Logging = LogSystem.Start(settings, SessionInfo.Collect(DateTime.Now), report => CrashPrompt?.Offer(report));
            _crashUploader = CreateCrashUploader(settings, uploads);
            Flow = new AppFlow();

            Application.quitting += Stop;
            Log.Info(LogApp, "启动完成，当前 " + Flow.State);
        }

        // 第一个场景加载之前：这时才能改 Unity 每帧要跑的步骤，也才能建常驻的物体。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeFirstScene()
        {
            UnityLogClock.TrackFrames();

            if (CrashPrompt != null && CrashPrompt.Enabled)
            {
                _crashPromptView = CrashPromptView.Create(CrashPrompt);
            }

            _crashUploader?.UploadPending();
        }

        // 只在打出来的包里上传（编辑器和包共用玩家数据目录，编辑器不该替包发）。
        // 没填 Sentry DSN 就不上传；填错了记一条错误，这次运行不上传。
        private static SentryCrashUploader CreateCrashUploader(LogSystemSettings settings, CrashUploadQueue uploads)
        {
            if (Application.isEditor)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(settings.SentryDsn))
            {
                Log.Info(LogApp, "没有设 Sentry DSN，玩家同意发送的崩溃包先留在本地");
                return null;
            }

            try
            {
                string environment = Debug.isDebugBuild ? "development" : "production";
                return new SentryCrashUploader(SentryDsn.Parse(settings.SentryDsn), uploads, Application.version, environment);
            }
            catch (ArgumentException exception)
            {
                Log.Exception(LogApp, exception, "Sentry DSN 不对，这次运行不上传崩溃包");
                return null;
            }
        }

        // 程序退出（编辑器里是停止运行）时由 Unity 调用。先后和 Start 相反：日志系统最后收，这样前面每一步都留得下日志。
        private static void Stop()
        {
            Application.quitting -= Stop;

            if (_crashPromptView != null)
            {
                UnityEngine.Object.Destroy(_crashPromptView.gameObject);
            }

            _crashPromptView = null;
            _crashUploader?.Dispose();
            _crashUploader = null;

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
            CrashPrompt = null;
        }
    }
}
