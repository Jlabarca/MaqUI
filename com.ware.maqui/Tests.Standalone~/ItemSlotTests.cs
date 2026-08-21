// SPDX-License-Identifier: MIT
// MaqUI v2 — ItemSlot / Grid / Tooltip (V2-PLAYER-UI VPU.4).

using System.Collections.Generic;
using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class ItemSlotTests
    {
        [Fact]
        public void ItemSlot_DrawsIconAndCount()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ItemSlot("s0", "Apple", count: 5);
            gui.EndFrame();

            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawImage && o.Text == "Apple");
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawText && o.Text == "5");
        }

        [Fact]
        public void ItemSlot_CountOfOne_DrawsNoCount()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ItemSlot("s0", "Apple", count: 1);
            gui.EndFrame();

            Assert.DoesNotContain(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawText);
        }

        [Fact]
        public void ItemSlot_Selected_SwapsClassName()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ItemSlot("s0", "Apple", selected: true, className: "slot", selectedClassName: "slot--sel");
            gui.EndFrame();

            var slotCol = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin);
            Assert.Equal("slot--sel", slotCol.ClassName);
        }

        [Fact]
        public void ItemSlot_NoInteraction_ReportsNeitherClickNorHover()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var r = gui.ItemSlot("s0", "Apple");
            gui.EndFrame();

            Assert.False(r.Clicked);
            Assert.False(r.Hovered);
        }

        [Fact]
        public void ItemSlot_NoInteraction_ReportsNoDrag()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var r = gui.ItemSlot("s0", "Apple");
            gui.EndFrame();

            Assert.False(r.DragStarted);
            Assert.False(r.Dropped);
        }

        [Fact]
        public void ItemSlot_EmptyIcon_DrawsNoImage()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ItemSlot("s0", null);
            gui.EndFrame();

            Assert.DoesNotContain(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawImage);
        }
    }

    public class GridTests
    {
        [Fact]
        public void Grid_InvokesRenderCellOncePerIndexInOrder()
        {
            var seen = new List<int>();
            var gui = new Gui();
            gui.BeginFrame();
            gui.Grid(4, 10, i => seen.Add(i));
            gui.EndFrame();

            Assert.Equal(Enumerable.Range(0, 10).ToList(), seen);
        }

        [Fact]
        public void Grid_WrapsIntoCeilRows()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Grid(4, 10, _ => { });   // 10 items / 4 cols = 3 rows (4+4+2)
            gui.EndFrame();

            int rows = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.RowBegin);
            Assert.Equal(3, rows);
        }

        [Fact]
        public void Grid_ZeroItems_IsNoOp()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Grid(4, 0, _ => Assert.Fail("should not render"));
            gui.EndFrame();
            Assert.Empty(gui.Buffer.Ops);
        }

        [Fact]
        public void Grid_ColumnsClampedToAtLeastOne()
        {
            int calls = 0;
            var gui = new Gui();
            gui.BeginFrame();
            gui.Grid(0, 3, _ => calls++);   // columns<1 → treated as 1 → 3 rows
            gui.EndFrame();

            Assert.Equal(3, calls);
            Assert.Equal(3, gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.RowBegin));
        }
    }

    public class TooltipTests
    {
        [Fact]
        public void Tooltip_Hidden_RendersNothing()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Tooltip(false, () => gui.DrawText("info"));
            gui.EndFrame();
            Assert.Empty(gui.Buffer.Ops);
        }

        [Fact]
        public void Tooltip_Shown_RendersContent()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Tooltip(true, () => gui.DrawText("info"), className: "tip");
            gui.EndFrame();

            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawText && o.Text == "info");
            var col = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin);
            Assert.Equal("tip", col.ClassName);
        }
    }
}
