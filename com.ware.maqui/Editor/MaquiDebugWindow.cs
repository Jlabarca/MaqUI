using System.Collections.Generic;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using UnityEditor;
using UnityEngine;

namespace Maqui.Editor
{
    /// <summary>
    /// Editor window that displays live Maqui framework state during Play Mode.
    /// Open via Window > Maqui > Debug Window.
    /// </summary>
    public class MaquiDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private readonly Dictionary<string, bool> _windowFoldouts = new();

        [MenuItem("Window/Maqui/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaquiDebugWindow>("Maqui Debug");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see Maqui debug info.", MessageType.Info);
                return;
            }

            var uiService = MaquiServices.Get<IUIService>();
            if (uiService == null || !(uiService is MaquiWindowManager manager))
            {
                EditorGUILayout.HelpBox("IUIService is not registered or is not a MaquiWindowManager.", MessageType.Warning);
                DrawServicesStatus();
                return;
            }

            var debugInfo = manager.GetDebugInfo();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawServicesStatus();
            EditorGUILayout.Space(8);
            DrawModalState(debugInfo);
            EditorGUILayout.Space(8);
            DrawNavigation(debugInfo, manager);
            EditorGUILayout.Space(8);
            DrawLayerOverview(debugInfo);

            EditorGUILayout.EndScrollView();
        }

        // ── Services Status ─────────────────────────────────────────────────────

        private void DrawServicesStatus()
        {
            EditorGUILayout.LabelField("Services Status", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                DrawServiceRow<IInputBridge>("IInputBridge");
                DrawServiceRow<IRouterBridge>("IRouterBridge");
                DrawServiceRow<IThemeProvider>("IThemeProvider");
                DrawServiceRow<IAnimationBridge>("IAnimationBridge");
                DrawServiceRow<IUIService>("IUIService");
            }
        }

        private void DrawServiceRow<T>(string label) where T : class
        {
            var instance = MaquiServices.Get<T>();
            bool registered = instance != null;

            var rect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
            var statusRect = new Rect(rect.xMax - 76, rect.y, 76, rect.height);

            EditorGUI.LabelField(labelRect, label);

            var prevColor = GUI.color;
            GUI.color = registered ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            EditorGUI.LabelField(statusRect, registered ? "Registered" : "Missing", EditorStyles.boldLabel);
            GUI.color = prevColor;
        }

        // ── Modal State ─────────────────────────────────────────────────────────

        private void DrawModalState(MaquiWindowManager.DebugInfo info)
        {
            EditorGUILayout.LabelField("Modal State", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Modal Count", info.ModalCount.ToString());
                EditorGUILayout.LabelField("Modal Mask", info.ModalMaskActive ? "Active" : "Inactive");

                var prevColor = GUI.color;
                GUI.color = info.IsFrozen ? new Color(1f, 0.7f, 0.2f) : Color.white;
                EditorGUILayout.LabelField("Freeze State", info.IsFrozen ? "FROZEN" : "Normal");
                GUI.color = prevColor;

                EditorGUILayout.LabelField("Freezable Views", info.FreezableViewCount.ToString());
            }
        }

        // ── Navigation ──────────────────────────────────────────────────────────

        private void DrawNavigation(MaquiWindowManager.DebugInfo info, MaquiWindowManager manager)
        {
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                var prevColor = GUI.color;
                GUI.color = info.SuppressBackNavigation ? new Color(1f, 0.7f, 0.2f) : Color.white;
                EditorGUILayout.LabelField("SuppressBackNavigation",
                    info.SuppressBackNavigation ? "SUPPRESSED" : "Normal");
                GUI.color = prevColor;

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Pop Topmost Window", GUILayout.Height(24)))
                {
                    UILayer[] layerPriority = { UILayer.Modal, UILayer.Overlay, UILayer.Default, UILayer.Background };
                    foreach (var layer in layerPriority)
                    {
                        if (info.StackDepths.TryGetValue(layer, out int depth) && depth > 0)
                        {
                            manager.PopWindow(layer);
                            break;
                        }
                    }
                }
            }
        }

        // ── Layer Overview ──────────────────────────────────────────────────────

        private void DrawLayerOverview(MaquiWindowManager.DebugInfo info)
        {
            EditorGUILayout.LabelField("Layer Overview", EditorStyles.boldLabel);

            UILayer[] layers = { UILayer.Background, UILayer.Default, UILayer.Overlay, UILayer.Modal };

            foreach (var layer in layers)
            {
                int stackDepth = info.StackDepths.TryGetValue(layer, out int d) ? d : 0;
                var windows = info.WindowsByLayer.TryGetValue(layer, out var list) ? list : null;
                int windowCount = windows?.Count ?? 0;

                EditorGUILayout.Space(2);

                // Layer header
                var headerRect = EditorGUILayout.GetControlRect(false, 20);
                var headerStyle = new GUIStyle(EditorStyles.boldLabel);

                if (windowCount > 0)
                    headerStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

                EditorGUI.LabelField(headerRect,
                    $"{layer}  (depth: {stackDepth})", headerStyle);

                if (windows == null || windows.Count == 0)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                    }
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    // Windows listed top-of-stack first (index 0 = top)
                    for (int i = 0; i < windows.Count; i++)
                    {
                        var w = windows[i];
                        string foldoutKey = $"{layer}_{i}_{w.AssetKey}";

                        if (!_windowFoldouts.ContainsKey(foldoutKey))
                            _windowFoldouts[foldoutKey] = false;

                        string prefix = i == 0 ? "[TOP] " : $"[{i}] ";
                        _windowFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                            _windowFoldouts[foldoutKey],
                            $"{prefix}{w.RootName}",
                            true);

                        if (_windowFoldouts[foldoutKey])
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.LabelField("Asset Key", w.AssetKey ?? "(none)");
                                EditorGUILayout.LabelField("Visible", w.IsVisible.ToString());
                                EditorGUILayout.LabelField("Pooled", w.IsPooled.ToString());
                                EditorGUILayout.LabelField("Root GO", w.RootName);
                            }
                        }
                    }
                }
            }
        }
    }
}
