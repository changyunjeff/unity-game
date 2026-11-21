using System.IO;
using Game.Core.Logging.CrashReports;
using Game.Engine;
using Game.Engine.Logging;
using UnityEditor;

namespace Game.EngineEditor.Logging
{
    /// <summary>编辑器顶部菜单 Game → 日志 下的三项。</summary>
    internal static class LogMenu
    {
        [MenuItem("Game/日志/打开日志目录")]
        private static void OpenLogDirectory() => OpenDirectory(LogSystemSettings.FromUnity().LogDirectory);

        [MenuItem("Game/日志/打开崩溃包目录")]
        private static void OpenCrashReportDirectory() => OpenDirectory(LogSystemSettings.FromUnity().CrashReportDirectory);

        [MenuItem("Game/日志/写一个崩溃包")]
        private static void WriteCrashReport() => EditorUtility.OpenWithDefaultApp(GameRuntime.Logging.WriteCrashReport(CrashReasons.Manual));

        // 崩溃包要用到运行时才有的最近记录，所以没在运行时这一项是灰的。
        [MenuItem("Game/日志/写一个崩溃包", true)]
        private static bool CanWriteCrashReport() => GameRuntime.Logging != null;

        private static void OpenDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
            EditorUtility.OpenWithDefaultApp(directory);
        }
    }
}
