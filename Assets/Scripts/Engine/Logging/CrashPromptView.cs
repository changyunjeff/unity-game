using System;
using Game.Core.Logging.CrashReports;
using UnityEngine;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 崩溃界面：有崩溃包等着玩家回答时，在画面正中弹一个窗口，问发不发。只画，要不要弹、选了之后怎么办都归 <see cref="CrashPrompt"/>。
    ///
    /// 现在用 Unity 自带的即时模式界面（OnGUI）画：它不需要场景、预制体和界面框架，崩溃时最不容易跟着出问题。
    /// 系统级场景和界面框架搬过来之后，换成正式的界面只换这一个类。
    /// </summary>
    internal sealed class CrashPromptView : MonoBehaviour
    {
        private const int WindowId = 0x4352; // 随便一个不和别的窗口撞的数
        private const float MaxWidth = 560f;
        private const float MaxHeight = 400f;
        private const float Margin = 20f;

        private CrashPrompt _prompt;
        private CrashReportWritten _shown;
        private Vector2 _scroll;
        private bool _cursorFreed;

        /// <summary>建一个常驻的界面，换场景也不销毁。</summary>
        public static CrashPromptView Create(CrashPrompt prompt)
        {
            if (prompt == null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            var host = new GameObject("CrashPrompt");
            DontDestroyOnLoad(host);

            var view = host.AddComponent<CrashPromptView>();
            view._prompt = prompt;
            return view;
        }

        private void OnGUI()
        {
            if (!_prompt.TryGetPending(out _shown))
            {
                _cursorFreed = false;
                return;
            }

            // 游戏可能把鼠标藏起来或锁住了，玩家得能点到按钮。
            if (!_cursorFreed)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                _cursorFreed = true;
            }

            float width = Mathf.Min(MaxWidth, Screen.width - Margin * 2);
            float height = Mathf.Min(MaxHeight, Screen.height - Margin * 2);
            var area = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);

            GUI.depth = -1000; // 画在别的 OnGUI 上面
            GUI.ModalWindow(WindowId, area, DrawWindow, "游戏出了点问题");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(Headline(_shown.Reason));
            GUILayout.Label("如果你同意，会把下面这些文件发给我们，帮我们找到原因。里面没有你的用户名。");

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (string file in _shown.Files)
            {
                GUILayout.Label("· " + file);
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("看看内容"))
            {
                Application.OpenURL(new Uri(_shown.Directory).AbsoluteUri);
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("发送"))
            {
                _prompt.Answer(CrashPromptChoice.Send);
            }

            if (GUILayout.Button("不发送"))
            {
                _prompt.Answer(CrashPromptChoice.DontSend);
            }

            if (GUILayout.Button("以后都不问"))
            {
                _prompt.Answer(CrashPromptChoice.NeverAsk);
            }

            GUILayout.EndHorizontal();
            GUILayout.Label("选完之后可以继续玩。");
        }

        private static string Headline(string reason)
        {
            return reason == CrashReasons.UncleanExit
                ? "上次游戏没有正常退出（可能是崩溃了）。我们把当时的记录存了下来。"
                : "游戏里出现了一个没有处理好的错误。我们把出错前后的记录存了下来。";
        }
    }
}
