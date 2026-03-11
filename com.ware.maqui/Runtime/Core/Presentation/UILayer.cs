namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Rendering layer for Maqui windows. Maps to dedicated Canvas sort orders.
    /// Aligns with HybridFrame's IPluginAPI.UILayer definition.
    /// </summary>
    public enum UILayer
    {
        /// <summary>Sort order 0. Skybox overlays, ambient FX, cutscene letterbox.</summary>
        Background = 0,

        /// <summary>Sort order 100. Main game panels: inventory, character, map, NPC dialog.</summary>
        Default = 100,

        /// <summary>Sort order 200. HUD elements: HP bars, minimap, hotbar, buffs.</summary>
        Overlay = 200,

        /// <summary>Sort order 300. Full-screen or blocking windows: dialogs, card game, plugin store.</summary>
        Modal = 300,
    }
}
