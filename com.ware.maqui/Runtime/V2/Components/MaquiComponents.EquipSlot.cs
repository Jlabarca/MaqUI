// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.EquipSlot (V2-PRO-SKIN.2.1).

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A paperdoll equipment tile: a recessed icon square next to a stacked
        /// uppercase slot label + item name. Empty slots draw no icon and render
        /// the item name as "—". <paramref name="mirrored"/> puts the icon on the
        /// right (for the right-hand column of the Equipment window, whose tiles
        /// face inward toward the center paperdoll).
        ///
        /// <para>Composition of <see cref="Icon"/> + DrawText inside a clickable
        /// <see cref="Gui.Row"/> — same hit-testing structure as
        /// <see cref="ItemSlot"/>, so it reports a <see cref="SlotResult"/> (a click
        /// = "unequip me"). No new framework op.</para>
        ///
        /// <para><see cref="SlotResult.Dropped"/> is always read (mirrors
        /// <see cref="ItemSlot"/>'s own unconditional <c>OnDragEnd()</c> call) so any
        /// EquipSlot can double as a drop target for an item dragged from elsewhere —
        /// the caller decides whether to act on it. <see cref="SlotResult.DragStarted"/>
        /// is deliberately left false: dragging a filled slot back OUT is a distinct,
        /// unbuilt feature (what would the drop even mean?), not implied by this.</para>
        /// </summary>
        public static SlotResult EquipSlot(this Gui gui, string key, string slotLabel,
            string iconKey, string itemName, bool mirrored = false, float iconSize = 34f,
            string className = null, string iconClassName = null, string labelClassName = null,
            string nameClassName = null)
        {
            bool empty = string.IsNullOrEmpty(itemName);

            var row = gui.Row(Size.Expand(), Size.Fit(), background: default,
                alignItems: AlignItems.Center, className: className);
            {
                if (!mirrored) IconTile(gui, iconKey, iconSize, iconClassName);

                gui.Column(Size.Expand(), Size.Fit(), background: default,
                    alignItems: mirrored ? AlignItems.End : AlignItems.Start);
                {
                    gui.DrawText(slotLabel ?? string.Empty, className: labelClassName);
                    gui.DrawText(empty ? "—" : itemName, className: nameClassName);
                }
                gui.EndColumn();

                if (mirrored) IconTile(gui, iconKey, iconSize, iconClassName);
            }
            gui.EndRow();

            return new SlotResult(row.OnClick(), row.IsHovered(), dragStarted: false, dropped: row.OnDragEnd());
        }

        // The recessed icon square. A separate Column so the tile keeps its fixed
        // box even when the slot is empty (no icon) — the layout must not reflow
        // between equipped and empty states.
        private static void IconTile(Gui gui, string iconKey, float size, string iconClassName)
        {
            gui.Column(Size.Pixels(size + 8f), Size.Pixels(size + 8f), background: default,
                alignItems: AlignItems.Center, className: iconClassName);
            {
                if (!string.IsNullOrEmpty(iconKey))
                    gui.Icon(iconKey, size);
            }
            gui.EndColumn();
        }
    }
}
