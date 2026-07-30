// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Hotbar (V2-PRO-SKIN.4.2).

namespace Maqui.V2.Components
{
    /// <summary>Outcome of a <see cref="MaquiComponents.Hotbar"/> this frame.
    /// Implicitly converts to the clicked-slot <c>int</c> so existing callers that
    /// captured only that (e.g. <c>int clicked = gui.Hotbar(...)</c>) keep compiling
    /// unchanged — V2-UI-PARITY.5.5.</summary>
    public readonly struct HotbarResult
    {
        public int Clicked { get; }
        public int DragStarted { get; }
        public int Dropped { get; }

        public HotbarResult(int clicked, int dragStarted, int dropped)
        {
            Clicked = clicked; DragStarted = dragStarted; Dropped = dropped;
        }

        public static implicit operator int(HotbarResult r) => r.Clicked;
    }

    public static partial class MaquiComponents
    {
        /// <summary>
        /// A single horizontal row of <paramref name="slotCount"/> numbered action
        /// slots (RO's 1-2-…-9-0 quickbar). Each slot shows its hotkey number and,
        /// if <paramref name="iconKeyForSlot"/> returns a key, an icon. Composes the
        /// same clickable <see cref="Gui.Column"/> hit-test as <see cref="ItemSlot"/>
        /// — no new op — now also reading drag-start/drop per slot (V2-UI-PARITY.5.5),
        /// mirroring <see cref="ItemSlot"/>/<see cref="EquipSlot"/>'s already-shipped
        /// triple composition (click + drag-start + drop) on one node.
        /// </summary>
        /// <param name="labelForSlot">Hotkey label for slot i; defaults to the RO
        /// 1..9,0 layout when null.</param>
        public static HotbarResult Hotbar(this Gui gui, string key, int slotCount,
            System.Func<int, string> iconKeyForSlot, System.Func<int, string> labelForSlot = null,
            float slotSize = 44f, string className = null, string slotClassName = null,
            string numberClassName = null)
        {
            int clicked = -1, dragStarted = -1, dropped = -1;

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
                    if (slot.OnDragStart()) dragStarted = i;
                    if (slot.OnDragEnd()) dropped = i;
                }
            }
            gui.EndRow();

            return new HotbarResult(clicked, dragStarted, dropped);
        }

        // RO quickbar labels: slots 0..8 → "1".."9", slot 9 → "0".
        private static string DefaultHotkey(int i) =>
            i == 9 ? "0" : (i + 1).ToString();
    }
}
