using UnityEditor;
using UnityEditor.SceneManagement;
using Maqui.Core.Logic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MaquiDemos.Editor
{
    public static class MaquiDemoSetup
    {
        [MenuItem("Maqui/Setup Demos/Setup All", priority = 0)]
        public static void SetupAll()
        {
            SetupDemo1Gallery();
            SetupDemo2GameUI();
            SetupDemo3FrostedHUD();
            Debug.Log("[Maqui] All demo scenes configured.");
        }

        [MenuItem("Maqui/Setup Demos/1 - Gallery (OneUI)", priority = 1)]
        public static void SetupDemo1Gallery()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/MaquiDemos/Scenes/Demo1_Gallery.unity",
                OpenSceneMode.Single);

            // Remove OneUI demo controllers anywhere in the hierarchy
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    string t = mb.GetType().Name;
                    if (t == "SceneController" || t == "ObjectInstaller")
                        Object.DestroyImmediate(mb);
                }
            }

            // Remove the empty UI root if present
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "UI" && root.transform.childCount == 0)
                    Object.DestroyImmediate(root);

            var existing1 = FindInScene(scene, "MaquiBootstrap");
            if (existing1 != null) Object.DestroyImmediate(existing1);
            new GameObject("MaquiBootstrap").AddComponent<Gallery.Demo1Starter>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Maqui] Demo1_Gallery setup complete.");
        }

        [MenuItem("Maqui/Setup Demos/2 - Game HUD (Pack)", priority = 2)]
        public static void SetupDemo2GameUI()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/MaquiDemos/Scenes/Demo2_GameUI.unity",
                OpenSceneMode.Single);

            // Disable Pack demo scripts (keep visuals intact), but skip our own Maqui starters
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    string ns = mb.GetType().Namespace ?? "";
                    if (ns.StartsWith("MaquiDemos")) continue; // don't disable our own scripts
                    string t = mb.GetType().FullName ?? "";
                    if (t.Contains("Demo") || t.Contains("Game.Controller"))
                        mb.enabled = false;
                }

            // Fix EventSystem: replace StandaloneInputModule with InputSystemUIInputModule
            FixEventSystem(scene);

            // Recreate MaquiBootstrap fresh to ensure starter is enabled
            var existing = FindInScene(scene, "MaquiBootstrap");
            if (existing != null) Object.DestroyImmediate(existing);
            new GameObject("MaquiBootstrap").AddComponent<GameUI.Demo2Starter>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Maqui] Demo2_GameUI setup complete.");
        }

        [MenuItem("Maqui/Setup Demos/3 - Frosted HUD (TranslucentImage)", priority = 3)]
        public static void SetupDemo3FrostedHUD()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/MaquiDemos/Scenes/Demo3_FrostedHUD.unity",
                OpenSceneMode.Single);

            // Fix EventSystem if needed
            FixEventSystem(scene);

            var existing3 = FindInScene(scene, "MaquiBootstrap");
            if (existing3 != null) Object.DestroyImmediate(existing3);
            new GameObject("MaquiBootstrap").AddComponent<Frosted.Demo3Starter>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Maqui] Demo3_FrostedHUD setup complete.");
        }

        [MenuItem("Maqui/Setup Demos/Add Scenes to Build Settings", priority = 10)]
        public static void AddScenesToBuildSettings()
        {
            var scenes = new[]
            {
                "Assets/MaquiDemos/Scenes/Demo1_Gallery.unity",
                "Assets/MaquiDemos/Scenes/Demo2_GameUI.unity",
                "Assets/MaquiDemos/Scenes/Demo3_FrostedHUD.unity",
            };

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var path in scenes)
            {
                bool already = false;
                foreach (var s in existing)
                    if (s.path == path) { already = true; break; }
                if (!already)
                    existing.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log("[Maqui] Demo scenes added to Build Settings.");
        }

        [MenuItem("Maqui/Setup Demos/Create Theme Assets", priority = 11)]
        public static void CreateThemeAssets()
        {
            const string folder = "Assets/Resources/Themes";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Themes");

            // Light theme
            var light = ScriptableObject.CreateInstance<ThemeData>();
            light.ThemeName      = "Light";
            light.PrimaryColor   = new Color(0.22f, 0.47f, 0.90f);
            light.SecondaryColor = new Color(0.55f, 0.35f, 0.85f);
            light.AccentColor    = new Color(0.95f, 0.65f, 0.15f);
            light.BackgroundMain     = new Color(0.97f, 0.97f, 0.98f);
            light.BackgroundOverlay  = new Color(1f, 1f, 1f, 0.92f);
            light.BackgroundContrast = new Color(0.12f, 0.12f, 0.15f);
            light.TextPrimary   = new Color(0.12f, 0.12f, 0.15f);
            light.TextSecondary = new Color(0.5f, 0.5f, 0.55f);
            light.TextInverse   = Color.white;
            light.Success = new Color(0.18f, 0.72f, 0.35f);
            light.Warning = new Color(0.95f, 0.75f, 0.10f);
            light.Danger  = new Color(0.90f, 0.22f, 0.30f);
            light.Info    = new Color(0.20f, 0.60f, 0.95f);
            AssetDatabase.CreateAsset(light, folder + "/Theme_Light.asset");

            // Dark theme
            var dark = ScriptableObject.CreateInstance<ThemeData>();
            dark.ThemeName      = "Dark";
            dark.PrimaryColor   = new Color(0.35f, 0.60f, 1.0f);
            dark.SecondaryColor = new Color(0.65f, 0.45f, 0.95f);
            dark.AccentColor    = new Color(1.0f, 0.75f, 0.25f);
            dark.BackgroundMain     = new Color(0.10f, 0.10f, 0.14f);
            dark.BackgroundOverlay  = new Color(0.15f, 0.15f, 0.20f, 0.95f);
            dark.BackgroundContrast = Color.white;
            dark.TextPrimary   = new Color(0.92f, 0.92f, 0.95f);
            dark.TextSecondary = new Color(0.60f, 0.60f, 0.65f);
            dark.TextInverse   = new Color(0.10f, 0.10f, 0.14f);
            dark.Success = new Color(0.30f, 0.85f, 0.50f);
            dark.Warning = new Color(1.0f, 0.85f, 0.25f);
            dark.Danger  = new Color(1.0f, 0.35f, 0.40f);
            dark.Info    = new Color(0.40f, 0.70f, 1.0f);
            AssetDatabase.CreateAsset(dark, folder + "/Theme_Dark.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[Maqui] Theme assets created at " + folder);
        }

        static void FixEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            // InputSystemUIInputModule type — resolved at runtime to avoid hard assembly ref
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            foreach (var root in scene.GetRootGameObjects())
            foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
            {
                var old = es.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);

                if (inputModuleType != null && es.GetComponent(inputModuleType) == null)
                    es.gameObject.AddComponent(inputModuleType);
            }
        }

        static GameObject FindInScene(UnityEngine.SceneManagement.Scene s, string name)
        {
            foreach (var r in s.GetRootGameObjects())
                if (r.name == name) return r;
            return null;
        }
    }
}
