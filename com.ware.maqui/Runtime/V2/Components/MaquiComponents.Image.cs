// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Image.
//
// =====================================================================
// MAQUIV2.8 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// References UnityEngine.Texture2D + (eventually) an IImageLoader backend
// extension that resolves a string key to a Texture2D. The standalone
// xUnit csproj does NOT shim Texture2D; this file is excluded via
// <Compile Remove>. Compile-checked only inside Unity Editor.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Texture-backed image. The supplied <paramref name="textureKey"/> is
        /// resolved to a <see cref="Texture2D"/> by the backend's image loader
        /// (Unity-side; <c>IImageLoader.Resolve(key) → Texture2D</c>); v0
        /// returns nothing useful headlessly — the rendered chrome is a
        /// placeholder.
        ///
        /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
        /// </summary>
        public static Node Image(this Gui gui, string textureKey, float width = 64f, float height = 64f)
        {
            var box = gui.Box(width: Size.Pixels(width), height: Size.Pixels(height));
            // v0 placeholder fill — the Unity backend extension reads the
            // textureKey from a side-channel (via the GuiDriver) and applies
            // it as a background image when the IImageLoader resolves.
            gui.DrawRect(new Color32(180, 180, 180, 255), Size.Pixels(width), Size.Pixels(height));
            gui.DrawText("[img: " + (textureKey ?? "<null>") + "]");
            return box;
        }
    }

    /// <summary>
    /// Backend extension contract for resolving image keys to textures.
    /// Implemented by the operator's <see cref="UIToolkitBackend"/>
    /// subclass (or a wrapper).
    ///
    /// <para><b>Status: interface sketch (blind draft).</b></para>
    /// </summary>
    public interface IImageLoader
    {
        /// <summary>
        /// Resolve <paramref name="key"/> to a texture. Returning null is OK —
        /// the component renders a placeholder.
        /// </summary>
        Texture2D Resolve(string key);
    }
}
