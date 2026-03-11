using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using UnityEngine;

namespace Maqui.Core
{
    /// <summary>
    /// Automatic initializer for Maqui Core systems.
    /// Runs before any scene loads — no scene setup required.
    /// </summary>
    public static class CoreBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            Debug.Log("[Maqui] Initializing Core Foundation...");

            var coreObj = new GameObject("Maqui_Core");
            Object.DontDestroyOnLoad(coreObj);

            // Core bridges
            coreObj.AddComponent<InputBridge>();
            coreObj.AddComponent<RouterBridge>();
            coreObj.AddComponent<ThemeProvider>();
            coreObj.AddComponent<AnimationBridge>();

            // Window system — builds 4 layer canvases + modal mask immediately on AddComponent
            coreObj.AddComponent<MaquiWindowManager>();

            Debug.Log("[Maqui] Core Foundation Ready.");
        }
    }
}
