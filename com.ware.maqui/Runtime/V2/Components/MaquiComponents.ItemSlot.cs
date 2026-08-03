// SPDX-License-Identifier: MIT
// MaqUI v2 — Item-window core: ItemSlot, Grid, Tooltip (V2-PLAYER-UI VPU.4).
//
// The reusable heart of every inventory-like window. All three are pure
// composition of existing ops — no new framework primitive — so they inherit
// the reconciler/interaction plumbing for free.
//
// Known scope (feeds the VPU.5 gap list / VPU.7 verdict): Tooltip is a *placed*
// panel the caller positions in the layout (e.g. below the grid), NOT a
// cursor-following floating overlay. A true overlay needs absolute positioning
// in panel space (also wanted by VPU.6's drag-ghost); deferred deliberately.

namespace Maqui.V2.Components
{
    /// <summary>Outcome of an <see cref="MaquiComponents.ItemSlot"/> this frame.</summary>
    public readonly struct SlotResult
    {
        public bool Clicked { get; }
        public bool Hovered { get; }
        /// <summary>A drag began on this slot this frame (pressed here, then moved).
        /// Use it to snapshot the slot's item into a drag session. (VPU.6)</summary>
        public bool DragStarted { get; }
        /// <summary>A drag was released over this slot this frame — the slot is the
        /// drop target. Consume the drag session and act on it. (VPU.6)</summary>
        public bool Dropped { get; }

        public SlotResult(bool clicked, bool hovered, bool dragStarted = false, bool dropped = false)
        {
            Clicked = clicked; Hovered = hovered; DragStarted = dragStarted; Dropped = dropped;
        }
    }

    public static partial class MaquiComponents
    {
        /// <summary>
        /// A single inventory cell: an <see cref="Icon"/> plus an optional stack
        /// count, with selected / disabled visual states and click + hover
        /// reporting. Returns a <see cref="SlotResult"/> — the caller decides what
        /// a click or hover means (select, use, show tooltip).
        ///
        /// <para>The slot is a clickable <see cref="Gui.Column"/> container (same
        /// hit-testing structure as <see cref="Button"/>). <paramref name="selected"/>
        /// swaps in <paramref name="selectedClassName"/>; <paramref name="disabled"/>
        /// suppresses the click (but still draws, so the grid doesn't reflow).</para>
        /// </summary>
        public static SlotResult ItemSlot(this Gui gui, string key, string iconKey,
            int count = 0, bool selected = false, bool disabled = false, float size = 40f,
            string className = null, string selectedClassName = null, string countClassName = null)
        {
            string cls = selected ? (selectedClassName ?? className) : className;

            var slot = gui.Column(Size.Pixels(size), Size.Pixels(size), background: default,
                alignItems: AlignItems.Center, className: cls);
            {
                // 4px inset (was 8): enough to keep the slot border and hover ring
                // readable around the icon, without spending a quarter of a 40px cell
                // on empty padding. The count below is expected to be positioned
                // absolutely by the host stylesheet (ORO: `.ro-slot__count`), so it
                // costs no height here; a host that leaves it in flow still renders,
                // it just stacks instead of overlaying.
                if (!string.IsNullOrEmpty(iconKey))
                    gui.Icon(iconKey, size - 4f);
                // Stacks of 1 (or unset) don't draw a count — matches RO.
                if (count > 1)
                    gui.DrawText(count.ToString(), className: countClassName);
            }
            gui.EndColumn();

            bool clicked = !disabled && slot.OnClick();
            bool hovered = slot.IsHovered();
            // A disabled slot can't source a drag, but CAN receive a drop (e.g. an
            // empty/locked target the caller validates).
            bool dragStarted = !disabled && slot.OnDragStart();
            bool dropped = slot.OnDragEnd();
            return new SlotResult(clicked, hovered, dragStarted, dropped);
        }

        /// <summary>
        /// Fixed-column wrap: lays <paramref name="itemCount"/> cells into rows of
        /// <paramref name="columns"/>, invoking <paramref name="renderCell"/> with
        /// each index. Virtualization is deferred (v0 renders every cell) — fine for
        /// the bag sizes real inventories use; documented for the verdict.
        /// </summary>
        public static void Grid(this Gui gui, int columns, int itemCount,
            System.Action<int> renderCell, string rowClassName = null)
        {
            if (renderCell == null || itemCount <= 0) return;
            if (columns < 1) columns = 1;

            for (int row = 0; row * columns < itemCount; row++)
            {
                gui.Row(Size.Fit(), Size.Fit(), background: default, className: rowClassName);
                {
                    for (int col = 0; col < columns; col++)
                    {
                        int idx = row * columns + col;
                        if (idx >= itemCount) break;
                        renderCell(idx);
                    }
                }
                gui.EndRow();
            }
        }

        /// <summary>
        /// A hover-revealed info panel. Renders <paramref name="content"/> inside a
        /// styled container only when <paramref name="show"/> is true; a no-op
        /// otherwise. The caller places it in the layout (typically below a Grid)
        /// and toggles it from a slot's <see cref="SlotResult.Hovered"/>.
        ///
        /// <para><b>Not a floating overlay.</b> It occupies real layout space where
        /// drawn — position it after the content it describes so a reveal doesn't
        /// shove the grid around. Cursor-anchored floating is the deferred variant.</para>
        /// </summary>
        public static void Tooltip(this Gui gui, bool show, System.Action content, string className = null)
        {
            if (!show || content == null) return;

            gui.Column(Size.Expand(), Size.Fit(), background: default, className: className);
            {
                content();
            }
            gui.EndColumn();
        }
    }
}
