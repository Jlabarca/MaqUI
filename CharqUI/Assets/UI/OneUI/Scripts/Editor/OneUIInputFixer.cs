using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DevsDaddy.OneUI.Editor
{
    /// <summary>
    /// Helper tool to migrate OneUI scenes to the New Input System.
    /// </summary>
    public static class OneUIInputFixer
    {
        [MenuItem("Tools/OneUI/Fix Input System Conflict")]
        public static void FixInputSystemConflict()
        {
            EventSystem[] eventSystems = GameObject.FindObjectsOfType<EventSystem>();
            
            if (eventSystems.Length == 0)
            {
                Debug.LogWarning("[OneUI] No EventSystem found in the active scene.");
                return;
            }

            int fixedCount = 0;
            foreach (var es in eventSystems)
            {
                var legacyModule = es.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    GameObject go = es.gameObject;
                    Object.DestroyImmediate(legacyModule);
                    
                    // Add the new InputSystemUIInputModule
                    var newModule = go.AddComponent<InputSystemUIInputModule>();
                    Debug.Log($"[OneUI] Successfully replaced legacy Input Module with InputSystemUIInputModule on {go.name}.", go);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("OneUI Input Fixer", 
                    $"{fixedCount} EventSystem(s) updated to New Input System.\n\nPlease save your scene.", "OK");
            }
            else
            {
                Debug.Log("[OneUI] All EventSystems are already using the New Input System or no legacy modules were found.");
            }
        }
    }
}
