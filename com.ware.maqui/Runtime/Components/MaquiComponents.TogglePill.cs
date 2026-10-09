// SPDX-License-Identifier: MIT
// MaqUI — Controls.TogglePill. Generated via /maqui-component skill 2026-05-14.

using UnityEngine;

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Pill-shaped on/off toggle. When clicked, the state flips and an
        /// animated knob slides between left (off) and right (on). Pill body
        /// is 80x32; off-state background is gray, on-state is green.
        /// </summary>
        public static bool TogglePill(this Gui gui, string key, bool state)
        {
            var pill = gui.Box(width: Size.Pixels(80f), height: Size.Pixels(32f));

            Color32 bgColor = state
                ? new Color32(80, 180, 100, 255)
                : new Color32(120, 120, 130, 255);
            gui.DrawRect(bgColor, Size.Pixels(80f), Size.Pixels(32f));

            // Knob slide animation: t=0 (off, left) to t=1 (on, right).
            // The Unity backend reads this slot on the same frame to position the knob.
            float targetT = state ? 1f : 0f;
            float t = gui.Animate(MaquiStrings.Suffixed(key, "-knob-t"), targetT);

            // Knob marker — Unity backend reads (key + "-knob-t") for absolute placement.
            gui.DrawRect(new Color32(245, 245, 250, 255), Size.Pixels(24f), Size.Pixels(24f));

            if (pill.OnClick())
            {
                state = !state;
            }
            return state;
        }
    }
}
