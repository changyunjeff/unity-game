using System;
using System.IO;
using Game.Core.Versioning;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EngineEditor.Versioning
{
    /// <summary>
    /// 版本号只有一个出处：工程根目录的 <c>version.txt</c>。
    /// 游戏运行时读到的版本（<c>Application.version</c>）来自 Unity 的 Player Settings，所以这里负责把
    /// <c>version.txt</c> 里的版本号抄进 Player Settings。人只改 <c>version.txt</c>，Player Settings 里那一格不用手改。
    ///
    /// 三个时机会抄：编辑器载入代码之后、点运行之前、打包之前。
    /// <c>version.txt</c> 不存在或写法不对：前两个时机在 Console 里报错；打包直接失败。
    /// </summary>
    internal sealed class VersionSync : IPreprocessBuildWithReport
    {
        private const string FileName = "version.txt";

        // Unity 的接口规定的名字，所以不是大写开头。数越小越先执行。
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!TrySync(out string problem))
            {
                throw new BuildFailedException(problem);
            }
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SyncOrComplain();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                SyncOrComplain();
            }
        }

        private static void SyncOrComplain()
        {
            if (!TrySync(out string problem))
            {
                Debug.LogError(problem);
            }
        }

        // 把 version.txt 里的版本号抄进 Player Settings。抄不了就说出原因。
        private static bool TrySync(out string problem)
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), FileName);

            try
            {
                string version = GameVersion.Parse(File.ReadAllText(path)).ToString();

                if (PlayerSettings.bundleVersion != version)
                {
                    Debug.Log("版本号从 " + PlayerSettings.bundleVersion + " 改成 " + version + "（来自 " + FileName + "）");
                    PlayerSettings.bundleVersion = version;
                }

                problem = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException)
            {
                problem = "读不出版本号（" + path + "）：" + exception.Message;
                return false;
            }
        }
    }
}
