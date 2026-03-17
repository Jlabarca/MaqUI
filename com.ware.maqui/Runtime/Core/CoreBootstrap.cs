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

            // Core bridges — registered into MaquiServices for testable access
            MaquiServices.Register<IInputBridge>(coreObj.AddComponent<InputBridge>());
            MaquiServices.Register<IRouterBridge>(coreObj.AddComponent<RouterBridge>());
            MaquiServices.Register<IThemeProvider>(coreObj.AddComponent<ThemeProvider>());
            MaquiServices.Register<IAnimationBridge>(coreObj.AddComponent<AnimationBridge>());

            // Window system — builds 4 layer canvases + modal mask immediately on AddComponent
            MaquiServices.Register<IUIService>(coreObj.AddComponent<MaquiWindowManager>());

            Debug.Log("[Maqui] Core Foundation Ready.");
        }
    }
}
