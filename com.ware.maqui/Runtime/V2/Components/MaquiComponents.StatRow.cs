// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.StatRow (V2-PRO-SKIN.1.3).

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// One line of a character sheet: a dim <paramref name="label"/> on the
        /// left, then the <paramref name="value"/> (and an optional accent
        /// <paramref name="bonus"/>) pushed to the right, with an optional −/+
        /// <see cref="Stepper"/> at the far end for point allocation. Both the
        /// ATTRIBUTES column (STR 50 +2 with stepper) and the SECONDARY column
        /// (ATK 110 +2, read-only) in the pro Character layout are this one row.
        ///
        /// <para>Pure composition of DrawText + the existing Stepper — no new
        /// framework op. Every visual is a caller-supplied class so the same row
        /// serves both columns.</para>
        /// </summary>
        /// <returns>The stepper delta chosen this frame (-1/0/+1); always 0 when
        /// <paramref name="showStepper"/> is false.</returns>
        public static int StatRow(this Gui gui, string key, string label, string value,
            string bonus = null, bool showStepper = false,
            string labelClassName = null, string valueClassName = null, string bonusClassName = null,
            string buttonClassName = null, bool canDecrement = true, bool canIncrement = true)
        {
            int delta = 0;

            gui.Row(Size.Expand(), Size.Fit(), background: default, alignItems: AlignItems.Center);
            {
                gui.DrawText(label ?? string.Empty, className: labelClassName);
                gui.Spacer(Size.Expand());
                gui.DrawText(value ?? string.Empty, className: valueClassName);

                if (!string.IsNullOrEmpty(bonus))
                    gui.DrawText(bonus, className: bonusClassName);

                if (showStepper)
                    delta = gui.Stepper(key + "-st", string.Empty,
                        buttonClassName: buttonClassName,
                        canDecrement: canDecrement, canIncrement: canIncrement);
            }
            gui.EndRow();

            return delta;
        }
    }
}
