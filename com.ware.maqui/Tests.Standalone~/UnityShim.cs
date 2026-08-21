// SPDX-License-Identifier: MIT
// Minimal UnityEngine shim — only the types Maqui Runtime sources actually
// reference. Lets `dotnet test` compile the v2 source files outside Unity.
//
// When Unity compiles the same sources, UnityEngine.dll is auto-referenced
// and provides the real types; this file is NOT visible to Unity (parent
// folder ends with `~` per Unity's package import rules).
//
// Keep this file in lockstep with the actual UnityEngine surface Runtime/
// reaches for. Today: Color32 (DrawRect/DrawText/DrawLine/DrawCircle) +
// Texture2D (IImageLoader interface signature).

namespace UnityEngine
{
    /// <summary>
    /// Shim of <c>UnityEngine.Color32</c>. Field-equivalent to Unity's struct
    /// (4 bytes packed as r/g/b/a). Equality + ToString suffice for v2's tests.
    /// </summary>
    public readonly struct Color32
    {
        public readonly byte r;
        public readonly byte g;
        public readonly byte b;
        public readonly byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override string ToString() => $"RGBA({r}, {g}, {b}, {a})";
    }

    /// <summary>
    /// Shim of <c>UnityEngine.Texture2D</c>. Only the type identity is needed
    /// by <c>Maqui.Components.IImageLoader.Resolve</c>; no methods are
    /// exercised headlessly (no real image loading runs outside Unity).
    /// </summary>
    public class Texture2D { }

    /// <summary>
    /// Shim of <c>UnityEngine.Sprite</c>. Only the type identity is needed by
    /// <c>Maqui.Components.ISpriteLoader.Resolve</c> (atlas-backed icons whose
    /// sub-rect a bare Texture2D can't express); no methods run headlessly.
    /// </summary>
    public class Sprite { }
}
