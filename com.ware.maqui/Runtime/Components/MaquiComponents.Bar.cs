// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Bar (V2-PRO-SKIN.1.2).

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// The HP/SP/EXP evolution of <see cref="ProgressBar"/>: a track with a
        /// percentage-width fill AND a value label centered over the whole bar
        /// (e.g. "5798 / 5798"). The label is the only structural addition — it is
        /// drawn as an absolutely-positioned sibling of the fill (via
        /// <paramref name="labelClassName"/>, which supplies
        /// <c>position:absolute; left:0; right:0</c>) so it overlays the fill
        /// instead of pushing it, and the fill's percentage width stays honest.
        ///
        /// <para>Purely visual — no interaction. Class names are passed explicitly
        /// (not derived) so callers keep full control of the track / fill / label
        /// styling per bar kind (red HP gradient, blue SP, empty EXP track).</para>
        /// </summary>
        /// <param name="value01">Fill fraction; clamped to [0,1] for the visual.</param>
        /// <param name="valueText">Centered overlay label; null draws no label.</param>
        public static void Bar(this Gui gui, string key, float value01, string valueText,
            string className = null, string fillClassName = null, string labelClassName = null,
            float height = 18f)
        {
            float v = value01 < 0f ? 0f : (value01 > 1f ? 1f : value01);

            // Track is a Row so the fill nests as a real child (same rule as
            // ProgressBar). Width fills the parent; height is fixed.
            gui.Row(Size.Expand(), Size.Pixels(height), className: className);
            {
                // Fill: Percentage width, height stretched by the Row. Must NOT be
                // height-Expand (that maps onto flexGrow and fights the width).
                gui.Box(Size.Percentage(v), default, className: fillClassName);

                // Overlay label — absolutely positioned by its class, so it sits on
                // top of the fill and does not disturb the percentage-width layout.
                if (valueText != null)
                    gui.DrawText(valueText, className: labelClassName);
            }
            gui.EndRow();
        }
    }
}
