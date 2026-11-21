using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

// Demo of the three UI art kits: switches style (one generated USS per style) and screen at runtime.
// Run a player with "-uikitShots <folder>" to save one screenshot per style and screen, then quit.
[RequireComponent(typeof(UIDocument))]
public sealed class UiKitDemo : MonoBehaviour
{
    [SerializeField] private StyleSheet[] themes;
    [SerializeField] private string[] themeNames;

    private static readonly string[] Screens = { "mainMenu", "pause", "loading" };

    private VisualElement root;
    private StyleSheet currentTheme;
    private int themeIndex;
    private string screenName = "mainMenu";

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
        }

        BuildSwitcher();
        ApplyTheme(0);
        ShowScreen(screenName);
        SetProgress(0.4f);

        var shotDir = ReadArg("-uikitShots");
        if (shotDir != null)
        {
            StartCoroutine(CaptureAll(shotDir));
        }
    }

    public void ApplyTheme(int index)
    {
        if (themes == null || themes.Length == 0)
        {
            return;
        }

        if (currentTheme != null)
        {
            root.styleSheets.Remove(currentTheme);
        }

        themeIndex = Mathf.Clamp(index, 0, themes.Length - 1);
        currentTheme = themes[themeIndex];
        root.styleSheets.Add(currentTheme);
        RefreshSwitcher();
    }

    public void ShowScreen(string name)
    {
        screenName = name;
        foreach (var s in Screens)
        {
            root.Q(s)?.EnableInClassList("hidden", s != name);
        }

        RefreshSwitcher();
    }

    public void SetProgress(float value)
    {
        var fill = root.Q("progressFill");
        if (fill != null)
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
        }
    }

    private void BuildSwitcher()
    {
        var bar = root.Q("switcher");
        if (bar == null)
        {
            return;
        }

        bar.Clear();
        for (var i = 0; i < (themes?.Length ?? 0); i++)
        {
            var index = i;
            var label = themeNames != null && i < themeNames.Length ? themeNames[i] : themes[i].name;
            var b = new Button(() => ApplyTheme(index)) { text = label, name = "theme" + i };
            b.AddToClassList("ui-switcher-btn");
            bar.Add(b);
        }

        foreach (var s in Screens)
        {
            var screen = s;
            var b = new Button(() => ShowScreen(screen)) { text = screen, name = "screen-" + s };
            b.AddToClassList("ui-switcher-btn");
            bar.Add(b);
        }
    }

    private void RefreshSwitcher()
    {
        for (var i = 0; i < (themes?.Length ?? 0); i++)
        {
            root.Q("theme" + i)?.EnableInClassList("ui-switcher-btn--on", i == themeIndex);
        }

        foreach (var s in Screens)
        {
            root.Q("screen-" + s)?.EnableInClassList("ui-switcher-btn--on", s == screenName);
        }
    }

    private IEnumerator CaptureAll(string dir)
    {
        Directory.CreateDirectory(dir);
        for (var t = 0; t < themes.Length; t++)
        {
            ApplyTheme(t);
            foreach (var s in Screens)
            {
                ShowScreen(s);
                for (var f = 0; f < 5; f++)
                {
                    yield return null;
                }

                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, themes[t].name + "_" + s + ".png"));
                yield return null;
            }
        }

        yield return null;
        yield return null;
        Application.Quit();
    }

    private static string ReadArg(string key)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == key)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
