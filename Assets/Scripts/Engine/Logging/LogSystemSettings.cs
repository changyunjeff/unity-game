using System;
using System.IO;
using UnityEngine;

namespace Game.Engine.Logging
{
    /// <summary>日志系统要用到的全部设定。装配点建好它，交给 <see cref="LogSystem"/>；日志系统自己不去问 Unity。</summary>
    public sealed class LogSystemSettings
    {
        /// <summary>日志文件所在的目录。</summary>
        public string LogDirectory { get; set; }

        /// <summary>日志目录里留最近几次运行的文件。</summary>
        public int KeepLogSessions { get; set; } = 5;

        /// <summary>单个日志文件最多写多少字节。</summary>
        public long MaxLogFileBytes { get; set; } = 10L * 1024 * 1024;

        /// <summary>崩溃包所在的目录。</summary>
        public string CrashReportDirectory { get; set; }

        /// <summary>留最近几个崩溃包。</summary>
        public int KeepCrashReports { get; set; } = 10;

        /// <summary>每个崩溃包都要带上的文件，例如 Unity 自己的 Player.log。</summary>
        public string[] CrashReportAttachments { get; set; } = Array.Empty<string>();

        /// <summary>玩家的用户目录。写崩溃包时把它从文字里换掉。</summary>
        public string UserProfilePath { get; set; }

        /// <summary>出现没人接住的异常时，要不要自动写一个崩溃包。</summary>
        public bool ReportOnException { get; set; }

        /// <summary>要不要检查"上次运行没正常退出"。</summary>
        public bool DetectUncleanExit { get; set; }

        /// <summary>"正在运行"的标记文件放在哪。</summary>
        public string RunningMarkerPath { get; set; }

        /// <summary>
        /// 照 Unity 的环境填好的设定。目录都在玩家数据目录下。
        /// 自动写崩溃包和检查上次退出只在打出来的包里开：编辑器里出异常、停止运行、重新编译都是家常便饭，开了只会堆出一堆没用的包。
        /// </summary>
        public static LogSystemSettings FromUnity()
        {
            string root = Application.persistentDataPath;
            bool isPlayer = !Application.isEditor;

            return new LogSystemSettings
            {
                LogDirectory = Path.Combine(root, "Logs"),
                CrashReportDirectory = Path.Combine(root, "CrashReports"),
                CrashReportAttachments = new[] { Application.consoleLogPath },
                UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ReportOnException = isPlayer,
                DetectUncleanExit = isPlayer,
                RunningMarkerPath = Path.Combine(root, "running.marker"),
            };
        }
    }
}
