// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Image / Icon.
//
// =====================================================================
// VPU.2 — validated. DrawImage renders through the UIToolkitBackend via
// an IImageLoader (loose Texture2D) OR an ISpriteLoader (atlas-backed
// Sprite, whose sub-rect a bare Texture2D can't express — the RO item
// icon case). The standalone xUnit csproj shims Texture2D + Sprite, so
// this file compiles headlessly; the render path is covered by IconTests.

using UnityEngine;

namespace Maqui.Components
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
        /// </summary>
        public static Node Image(this Gui gui, string textureKey, float width = 64f, float height = 64f)
        {
            // Records a DrawImage FrameOp; UIToolkitBackend resolves the key via
            // its ISpriteLoader first (atlas sub-rect), then its IImageLoader
            // (loose Texture2D), else renders as an empty box at the given size.
            return gui.DrawImage(textureKey, width, height);
        }

        /// <summary>
        /// Square convenience over <see cref="Image"/>: an item/skill/status
        /// icon at <paramref name="size"/>×<paramref name="size"/>. The backend
        /// preserves the source aspect within the box (letterboxes a
        /// non-square sprite rather than stretching it).
        /// </summary>
        public static Node Icon(this Gui gui, string spriteKey, float size = 32f)
            => gui.DrawImage(spriteKey, size, size);
    }

    /// <summary>
    /// Backend extension contract for resolving image keys to atlas-backed
    /// <see cref="Sprite"/>s. Preferred over <see cref="IImageLoader"/> when a
    /// key names a sprite packed in an atlas: the sprite carries the sub-rect
    /// UI Toolkit needs, whereas a bare <see cref="Texture2D"/> would draw the
    /// whole atlas sheet. Implemented host-side (RO item icons resolve through
    /// the client's icon atlas). Returning null is OK — the component renders a
    /// placeholder, or falls through to the texture loader.
    /// </summary>
    public interface ISpriteLoader
    {
        /// <summary>Resolve <paramref name="key"/> to a sprite, or null.</summary>
        Sprite Resolve(string key);
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
