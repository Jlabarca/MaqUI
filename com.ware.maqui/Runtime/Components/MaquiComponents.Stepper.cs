// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Stepper (V2-PLAYER-UI VPU.1).

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A −/value/+ row for allocating a discrete amount (RO stat points, drop
        /// count, refine level). Returns the delta chosen THIS frame: -1, 0, or +1.
        /// The caller owns the value and clamping — the stepper only reports intent.
        ///
        /// <para>Pure composition of existing Buttons + DrawText, so it needs no new
        /// framework op. Style the buttons with <paramref name="buttonClassName"/>
        /// and the value label with <paramref name="labelClassName"/>.</para>
        /// </summary>
        public static int Stepper(this Gui gui, string key, string valueText,
            string buttonClassName = null, string labelClassName = null, bool canDecrement = true, bool canIncrement = true)
        {
            int delta = 0;

            gui.Row(Size.Fit(), Size.Fit(), background: default, alignItems: AlignItems.Center);
            {
                // The disabled sides still draw a button (so the row doesn't jump)
                // but their click is ignored — cheaper and steadier than conditionally
                // omitting the node, which would change sibling identity.
                bool minus = gui.Button("−", key: key + "-dec", className: buttonClassName);
                if (minus && canDecrement) delta = -1;

                gui.DrawText(valueText ?? string.Empty, className: labelClassName);

                bool plus = gui.Button("+", key: key + "-inc", className: buttonClassName);
                if (plus && canIncrement) delta = +1;
            }
            gui.EndRow();

            return delta;
        }
    }
}
