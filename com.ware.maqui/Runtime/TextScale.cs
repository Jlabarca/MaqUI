// SPDX-License-Identifier: MIT
// MaqUI v2 — text-size ramp (V2-UI-PARITY.3.2).
//
// Gui.DrawText's fontSize argument is a fixed float at every call site — the
// direct cause of the "fixed font size, shrink-till-overlap" report on
// Character and HUD. This is a pure C# helper a caller invokes BEFORE calling
// DrawText; no Gui/FrameOp/UIToolkitBackend involvement, so it's headlessly
// testable in full (unlike 3.1/3.5, which need the Unity Editor).
//
// System.Math, not UnityEngine.Mathf — UnityShim.cs (the standalone test
// project's UnityEngine shim) does not cover Mathf.

using System;

namespace Maqui
{
    public static class TextScale
    {
        /// <summary>
        /// Scales <paramref name="baseFontSize"/> linearly against
        /// <paramref name="availableWidth"/> relative to <paramref name="referenceWidth"/>,
        /// clamped to <c>[minFontSize, maxFontSize]</c>. At exactly
        /// <paramref name="referenceWidth"/> the result equals <paramref name="baseFontSize"/>
        /// (clamped, if <paramref name="baseFontSize"/> itself sits outside the clamp range).
        /// </summary>
        public static float Ramp(float availableWidth, float baseFontSize,
            float minFontSize = 10f, float maxFontSize = 24f, float referenceWidth = 400f)
        {
            if (referenceWidth <= 0f) return Math.Clamp(baseFontSize, minFontSize, maxFontSize);

            float width = Math.Max(0f, availableWidth);
            float scaled = baseFontSize * (width / referenceWidth);
            return Math.Clamp(scaled, minFontSize, maxFontSize);
        }
    }
}
