using Maqui.Core.Presentation;
using UnityEditor;
using UnityEngine;

namespace Maqui.Editor
{
    /// <summary>
    /// Editor-time validation tool for Maqui view prefabs.
    /// Scans Resources/Views/ for prefabs and checks they have required components.
    /// </summary>
    public static class MaquiAssetValidator
    {
        [MenuItem("Maqui/Validate Asset Keys")]
        public static void ValidateAssetKeys()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Views" });

            if (guids.Length == 0)
            {
                Debug.Log("[Maqui] Validator: No prefabs found in Assets/Resources/Views/.");
                return;
            }

            int valid = 0;
            int invalid = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    Debug.LogWarning($"[Maqui] Validator: Could not load prefab at '{path}'.");
                    invalid++;
                    continue;
                }

                bool hasIssues = false;

                // Check for CanvasGroup on root
                if (prefab.GetComponent<CanvasGroup>() == null)
                {
                    Debug.LogWarning($"[Maqui] Validator: Prefab '{path}' missing CanvasGroup on root. ReactiveBaseView requires CanvasGroup.");
                    hasIssues = true;
                }

                // Check for at least one MaquiBaseView-derived component
                var view = prefab.GetComponent<MaquiBaseView>();
                if (view == null)
                {
                    Debug.LogWarning($"[Maqui] Validator: Prefab '{path}' has no MaquiBaseView (or ReactiveBaseView) component. This prefab cannot be used with ShowWindowAsync.");
                    hasIssues = true;
                }

                // Check for Canvas on root (anti-pattern)
                if (prefab.GetComponent<Canvas>() != null)
                {
                    Debug.LogWarning($"[Maqui] Validator: Prefab '{path}' has a Canvas on root. This is an anti-pattern — remove it. Views are parented to layer canvases.");
                    hasIssues = true;
                }

                if (hasIssues)
                    invalid++;
                else
                    valid++;
            }

            Debug.Log($"[Maqui] Validator: Scanned {guids.Length} prefabs — {valid} valid, {invalid} with issues.");
        }
    }
}
