// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Toggle.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Pill-shaped on/off toggle. Returns the (possibly flipped) state
        /// after processing this frame's pointer interactions. When clicked,
        /// the state flips and the knob animation slides.
        ///
        /// <para>Animation slot uses <paramref name="key"/> + suffix so multiple
        /// toggles on the same screen coexist.</para>
        /// </summary>
        public static bool Toggle(this Gui gui, string key, bool state)
        {
            // Pill body: 80×32 px.
            var pill = gui.Box(width: Size.Pixels(80f), height: Size.Pixels(32f));

            Color32 bgColor = state
                ? new Color32(80, 180, 100, 255)   // on — green
                : new Color32(120, 120, 130, 255); // off — gray
            gui.DrawRect(bgColor, Size.Pixels(80f), Size.Pixels(32f));

            // Knob position animates between t=0 (off) and t=1 (on).
            float targetT = state ? 1f : 0f;
            float t = gui.Animate(key + "-toggle-t", targetT);

            // Knob marker — Unity backend reads (key + "-toggle-t") to position absolutely.
            gui.DrawRect(new Color32(245, 245, 250, 255), Size.Pixels(24f), Size.Pixels(24f));

            // Click flips state.
            if (pill.OnClick())
            {
                state = !state;
            }
            return state;
        }
    }
}
