// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.ProgressBar (V2-PLAYER-UI VPU.1).

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Horizontal fill bar. A track (styled via <paramref name="className"/>)
        /// containing a fill (styled via <paramref name="fillClassName"/>) whose
        /// width is <paramref name="value01"/> of the track. The fill uses a real
        /// Percentage size, so the bar tracks its parent with no caller-supplied
        /// pixel width.
        ///
        /// <para>Purely visual — no interaction. HP/SP/EXP and cast bars are the
        /// target: RO's most ubiquitous non-icon element. Class names are passed
        /// explicitly (not derived) so callers keep full control of styling.</para>
        /// </summary>
        /// <param name="value01">Fill fraction; clamped to [0,1] for the visual.</param>
        public static void ProgressBar(this Gui gui, string key, float value01,
            string className = null, string fillClassName = null, float height = 14f)
        {
            float v = value01 < 0f ? 0f : (value01 > 1f ? 1f : value01);

            // Track is a Row so the fill nests as a real child (Box is leaf-only and
            // wouldn't parent it — same rule as Button/Toggle). Width fills the
            // parent; height is fixed.
            gui.Row(Size.Expand(), Size.Pixels(height), className: className);
            {
                // Fill: Percentage width, height left to the Row's default stretch
                // (alignItems: Stretch). Height must NOT be Expand — the backend maps
                // a height-Expand onto flexGrow, which would fight the width.
                gui.Box(Size.Percentage(v), default, className: fillClassName);
            }
            gui.EndRow();
        }
    }
}
