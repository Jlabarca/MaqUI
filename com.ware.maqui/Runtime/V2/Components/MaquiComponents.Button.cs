// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Button.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Click-to-trigger button. Composes a <see cref="Gui.Box"/> with a
        /// label inside; returns <c>true</c> on the frame the button is
        /// released (via <see cref="NodeInteractions.OnClick"/>).
        ///
        /// <para>Visual chrome (rounded corners, hover/active color tween) is
        /// optional polish — v0 emits a flat background. Hover/active state is
        /// readable via the interaction flags on the returned node if callers
        /// want to render highlights themselves.</para>
        ///
        /// <para>Optional <paramref name="key"/> is the animation slot identity
        /// for future hover-scale tween work; unused at v0.</para>
        /// </summary>
        public static bool Button(this Gui gui, string label, string key = null)
        {
            // Default visual: 32-px-tall padded box with the label centered.
            var box = gui.Box(height: Size.Pixels(32f));
            gui.DrawRect(new Color32(60, 100, 160, 255));
            gui.DrawText(label ?? string.Empty, new Color32(240, 240, 240, 255));
            return box.OnClick();
        }
    }
}
