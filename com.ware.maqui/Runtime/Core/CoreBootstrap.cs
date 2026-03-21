using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using UnityEngine;

namespace Maqui.Core
{
    /// <summary>
    /// Automatic initializer for Maqui Core systems.
    /// Runs before any scene loads — no scene setup required.
    /// Optionally configured via a MaquiConfig asset in Resources/.
    /// </summary>
    public static class CoreBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            var config = Resources.Load<MaquiConfig>("MaquiConfig");
            Initialize(config);
        }

        /// <summary>
        /// Initialize with an explicit config. Passing null uses all-enabled defaults.
        /// Exposed for testing — production code uses the parameterless overload.
        /// </summary>
        public static void Initialize(MaquiConfig config)
        {
            Debug.Log("[Maqui] Initializing Core Foundation...");

            var coreObj = new GameObject("Maqui_Core");
            Object.DontDestroyOnLoad(coreObj);

            bool headless = config != null && config.Headless;
            bool enableInput = config == null || config.EnableInput;
            bool enableRouter = config == null || config.EnableRouter;
            bool enableTheme = config == null || config.EnableTheme;
            bool enableAnimation = config == null || config.EnableAnimation;
            bool enableWindowManager = config == null || config.EnableWindowManager;

            if (headless)
            {
                Debug.Log("[Maqui] Headless mode — skipping all bridges and window manager.");
                Debug.Log("[Maqui] Core Foundation Ready (headless).");
                return;
            }

            // Core bridges — registered into MaquiServices for testable access
            if (enableInput)
                MaquiServices.Register<IInputBridge>(coreObj.AddComponent<InputBridge>());
            else
                Debug.Log("[Maqui] Skipping InputBridge (disabled in config).");

            if (enableRouter)
                MaquiServices.Register<IRouterBridge>(coreObj.AddComponent<RouterBridge>());
            else
                Debug.Log("[Maqui] Skipping RouterBridge (disabled in config).");

            if (enableTheme)
                MaquiServices.Register<IThemeProvider>(coreObj.AddComponent<ThemeProvider>());
            else
                Debug.Log("[Maqui] Skipping ThemeProvider (disabled in config).");

            if (enableAnimation)
                MaquiServices.Register<IAnimationBridge>(coreObj.AddComponent<AnimationBridge>());
            else
                Debug.Log("[Maqui] Skipping AnimationBridge (disabled in config).");

            // Window system — builds 4 layer canvases + modal mask immediately on AddComponent
            if (enableWindowManager)
                MaquiServices.Register<IUIService>(coreObj.AddComponent<MaquiWindowManager>());
            else
                Debug.Log("[Maqui] Skipping MaquiWindowManager (disabled in config).");

            Debug.Log("[Maqui] Core Foundation Ready.");
        }
    }
}
