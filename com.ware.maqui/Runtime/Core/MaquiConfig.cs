using UnityEngine;

namespace Maqui.Core
{
    /// <summary>
    /// Optional configuration for Maqui bootstrap.
    /// Place in Resources/ as "MaquiConfig" to customize which bridges are created.
    /// If no asset exists, all services are enabled (backward-compatible default).
    /// </summary>
    [CreateAssetMenu(fileName = "MaquiConfig", menuName = "Maqui/Config")]
    public class MaquiConfig : ScriptableObject
    {
        [Header("Bootstrap")]
        [Tooltip("Skip all bridge and window manager creation. Use for headless/server builds or tests.")]
        public bool Headless = false;

        [Header("Bridges")]
        [Tooltip("Create InputBridge on bootstrap")]
        public bool EnableInput = true;

        [Tooltip("Create RouterBridge on bootstrap")]
        public bool EnableRouter = true;

        [Tooltip("Create ThemeProvider on bootstrap")]
        public bool EnableTheme = true;

        [Tooltip("Create AnimationBridge on bootstrap")]
        public bool EnableAnimation = true;

        [Tooltip("Create MaquiWindowManager on bootstrap")]
        public bool EnableWindowManager = true;
    }
}
