// SPDX-License-Identifier: MIT
// MaqUI v2 — Hotbar drag/drop tests (V2-UI-PARITY.5.5, headless).
//
// Mirrors DragTests.cs's SetFlags-before-call pattern: each Hotbar slot is a
// Column minted via NewNodeId(), so its id is predictable from
// Gui.PeekNextNodeId() as long as the node-id sequence within the call is
// known. A single-slot, icon-less Hotbar keeps that sequence simple: the Row
// itself takes the peeked id, slot 0's Column takes peeked+1, its label
// DrawText takes peeked+2 — so the slot's own id (the one OnDragStart/
// OnDragEnd read) is always peeked+1 for this shape.

using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class HotbarDragTests
    {
        [Fact]
        public void DragStarted_FlagOnSlot_ReportsThatSlotIndex()
        {
            var gui = new Gui();
            gui.BeginFrame();

            int slotNodeId = gui.PeekNextNodeId() + 1; // Row takes the peeked id, slot 0's Column takes the next.
            gui.Interactions.SetFlags(slotNodeId, NodeInteractionFlags.DragStartedThisFrame);

            var result = gui.Hotbar("hb", 1, _ => null);
            gui.EndFrame();

            Assert.Equal(0, result.DragStarted);
            Assert.Equal(-1, result.Dropped);
            Assert.Equal(-1, result.Clicked);
        }

        [Fact]
        public void Dropped_FlagOnSlot_ReportsThatSlotIndex()
        {
            var gui = new Gui();
            gui.BeginFrame();

            int slotNodeId = gui.PeekNextNodeId() + 1;
            gui.Interactions.SetFlags(slotNodeId, NodeInteractionFlags.DragEndedThisFrame);

            var result = gui.Hotbar("hb", 1, _ => null);
            gui.EndFrame();

            Assert.Equal(0, result.Dropped);
            Assert.Equal(-1, result.DragStarted);
        }

        [Fact]
        public void NoFlags_AllFieldsMinusOne()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var result = gui.Hotbar("hb", 3, _ => null);
            gui.EndFrame();

            Assert.Equal(-1, result.Clicked);
            Assert.Equal(-1, result.DragStarted);
            Assert.Equal(-1, result.Dropped);
        }

        [Fact]
        public void ImplicitIntConversion_YieldsClicked()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int clicked = gui.Hotbar("hb", 3, _ => null);
            gui.EndFrame();

            Assert.Equal(-1, clicked);
        }
    }
}
