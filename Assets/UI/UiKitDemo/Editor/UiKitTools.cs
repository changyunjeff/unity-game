using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// Generates one USS per art kit under Assets/Art/UI/<style>/ (any folder with a style.json),
// and builds the demo scene that switches between them.
public static class UiKitTools
{
    private const string ArtRoot = "Assets/Art/UI";
    private const string ThemeDir = "Assets/UI/Themes";
    private const string DemoDir = "Assets/UI/UiKitDemo";
    private const string ScenePath = "Assets/Scenes/UiKitDemo.unity";

    [System.Serializable]
    private sealed class StyleInfo
    {
        public string id;
        public string displayName;
        public bool pixel;
        public string dark;
        public string light;
        public string primary;
        public string accent;
    }

    [MenuItem("Game/UI/Generate Theme Styles")]
    public static void GenerateThemes()
    {
        Directory.CreateDirectory(ThemeDir);
        foreach (var dir in Directory.GetDirectories(ArtRoot))
        {
            var jsonPath = Path.Combine(dir, "style.json");
            if (!File.Exists(jsonPath))
            {
                continue;
            }

            var info = JsonUtility.FromJson<StyleInfo>(File.ReadAllText(jsonPath));
            var folder = ArtRoot + "/" + Path.GetFileName(dir);
            File.WriteAllText(ThemeDir + "/" + info.id + ".uss", BuildUss(folder, info), new UTF8Encoding(false));
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Game/UI/Build UI Kit Demo Scene")]
    public static void BuildDemoScene()
    {
        GenerateThemes();

        // A new scene unloads unused assets, so create it before loading any asset references.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var tssPath = DemoDir + "/UiKitBase.tss";
        if (!File.Exists(tssPath))
        {
            File.WriteAllText(tssPath, "@import url(\"project://database/Assets/UI/UiKitDemo/Common.uss\");\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(tssPath);
        }

        var panelPath = DemoDir + "/UiKitPanelSettings.asset";
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, panelPath);
        }

        panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(tssPath);
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1920, 1080);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 0.5f;
        EditorUtility.SetDirty(panel);

        var themes = AssetDatabase.FindAssets("t:StyleSheet", new[] { ThemeDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".uss"))
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<StyleSheet>)
            .ToArray();

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        var go = new GameObject("UI", typeof(UIDocument), typeof(UiKitDemo));
        var doc = go.GetComponent<UIDocument>();
        doc.panelSettings = panel;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DemoDir + "/UiKitDemo.uxml");

        var demo = new SerializedObject(go.GetComponent<UiKitDemo>());
        var themeProp = demo.FindProperty("themes");
        var nameProp = demo.FindProperty("themeNames");
        themeProp.arraySize = themes.Length;
        nameProp.arraySize = themes.Length;
        for (var i = 0; i < themes.Length; i++)
        {
            themeProp.GetArrayElementAtIndex(i).objectReferenceValue = themes[i];
            nameProp.GetArrayElementAtIndex(i).stringValue = DisplayName(themes[i].name);
        }

        demo.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static string DisplayName(string id)
    {
        foreach (var dir in Directory.GetDirectories(ArtRoot))
        {
            var jsonPath = Path.Combine(dir, "style.json");
            if (File.Exists(jsonPath))
            {
                var info = JsonUtility.FromJson<StyleInfo>(File.ReadAllText(jsonPath));
                if (info.id == id)
                {
                    return string.IsNullOrEmpty(info.displayName) ? id : info.displayName;
                }
            }
        }

        return id;
    }

    private static string BuildUss(string folder, StyleInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* Generated by Game/UI/Generate Theme Styles from " + folder + ". Do not edit by hand. */");
        sb.AppendLine();
        Rule(sb, ".ui-root", "color: " + info.dark + ";");

        Image(sb, ".ui-bg-main-menu", folder, "bg_main_menu");
        Image(sb, ".ui-bg-loading", folder, "bg_loading");
        Sliced(sb, ".ui-panel", folder, "panel_main", info);
        Sliced(sb, ".ui-dialog", folder, "panel_dialog", info);
        Sliced(sb, ".ui-banner-title", folder, "banner_title", info);
        Sliced(sb, ".ui-logo-frame", folder, "logo_frame", info);
        Sliced(sb, ".ui-progress-frame", folder, "progress_frame", info);
        Sliced(sb, ".ui-progress-fill", folder, "progress_fill", info);

        foreach (var kind in new[] { "primary", "secondary", "icon" })
        {
            var cls = ".ui-btn-" + kind;
            Sliced(sb, cls, folder, "button_" + kind + "_normal", info);
            Image(sb, cls + ":hover", folder, "button_" + kind + "_hover");
            Image(sb, cls + ":active", folder, "button_" + kind + "_pressed");
            Image(sb, cls + ":disabled", folder, "button_" + kind + "_disabled");
        }

        foreach (var icon in Directory.GetFiles(folder, "icon_*.png").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n))
        {
            Image(sb, ".ui-" + icon.Replace('_', '-'), folder, icon);
        }

        Sliced(sb, ".ui-slider .unity-base-slider__tracker", folder, "slider_track", info);
        Image(sb, ".ui-slider .unity-base-slider__dragger", folder, "slider_handle");
        Image(sb, ".ui-toggle .unity-toggle__checkmark", folder, "toggle_off");
        Image(sb, ".ui-toggle:checked .unity-toggle__checkmark", folder, "toggle_on");
        return sb.ToString();
    }

    private static void Image(StringBuilder sb, string selector, string folder, string file)
    {
        var path = folder + "/" + file + ".png";
        if (!File.Exists(path))
        {
            return;
        }

        Rule(sb, selector, "background-image: url(\"project://database/" + path + "\");");
    }

    private static void Sliced(StringBuilder sb, string selector, string folder, string file, StyleInfo info)
    {
        var path = folder + "/" + file + ".png";
        if (!File.Exists(path))
        {
            return;
        }

        var body = new StringBuilder();
        body.Append("background-image: url(\"project://database/" + path + "\");");
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var b = importer != null ? importer.spriteBorder : Vector4.zero;
        if (b != Vector4.zero)
        {
            // spriteBorder is (left, bottom, right, top).
            body.Append(" -unity-slice-left: " + (int)b.x + ";");
            body.Append(" -unity-slice-right: " + (int)b.z + ";");
            body.Append(" -unity-slice-top: " + (int)b.w + ";");
            body.Append(" -unity-slice-bottom: " + (int)b.y + ";");
        }

        var lum = CenterLuminance(path);
        if (lum >= 0f)
        {
            body.Append(" color: " + (lum > 0.55f ? info.dark : info.light) + ";");
        }

        Rule(sb, selector, body.ToString());
    }

    private static float CenterLuminance(string path)
    {
        var tex = new Texture2D(2, 2);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                return -1f;
            }

            float sum = 0f;
            var n = 0;
            for (var y = tex.height / 3; y < tex.height * 2 / 3; y += 2)
            {
                for (var x = tex.width / 3; x < tex.width * 2 / 3; x += 2)
                {
                    var c = tex.GetPixel(x, y);
                    if (c.a < 0.5f)
                    {
                        continue;
                    }

                    sum += 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                    n++;
                }
            }

            return n == 0 ? -1f : sum / n;
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    private static void Rule(StringBuilder sb, string selector, string body)
    {
        sb.AppendLine(selector + " {");
        foreach (var part in body.Split(';').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            sb.AppendLine("    " + part.ToString(CultureInfo.InvariantCulture) + ";");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }
}
