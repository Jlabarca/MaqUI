// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Hotbar (V2-PRO-SKIN.4.2).

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A single horizontal row of <paramref name="slotCount"/> numbered action
        /// slots (RO's 1-2-…-9-0 quickbar). Each slot shows its hotkey number and,
        /// if <paramref name="iconKeyForSlot"/> returns a key, an icon. Returns the
        /// index of the slot clicked this frame, or -1. Composes the same clickable
        /// <see cref="Gui.Column"/> hit-test as <see cref="ItemSlot"/> — no new op.
        /// </summary>
        /// <param name="labelForSlot">Hotkey label for slot i; defaults to the RO
        /// 1..9,0 layout when null.</param>
        public static int Hotbar(this Gui gui, string key, int slotCount,
            System.Func<int, string> iconKeyForSlot, System.Func<int, string> labelForSlot = null,
            float slotSize = 44f, string className = null, string slotClassName = null,
            string numberClassName = null)
        {
            int clicked = -1;

            gui.Row(Size.Fit(), Size.Fit(), background: default,
                alignItems: AlignItems.Center, className: className);
            {
                for (int i = 0; i < slotCount; i++)
                {
                    var slot = gui.Column(Size.Pixels(slotSize), Size.Pixels(slotSize),
                        background: default, alignItems: AlignItems.Center, className: slotClassName);
                    {
                        string label = labelForSlot != null ? labelForSlot(i) : DefaultHotkey(i);
                        gui.DrawText(label, className: numberClassName);

                        string iconKey = iconKeyForSlot?.Invoke(i);
                        if (!string.IsNullOrEmpty(iconKey))
                            gui.Icon(iconKey, slotSize - 18f);
                    }
                    gui.EndColumn();

                    if (slot.OnClick()) clicked = i;
                }
            }
            gui.EndRow();

            return clicked;
        }

        // RO quickbar labels: slots 0..8 → "1".."9", slot 9 → "0".
        private static string DefaultHotkey(int i) =>
            i == 9 ? "0" : (i + 1).ToString();
    }
}
