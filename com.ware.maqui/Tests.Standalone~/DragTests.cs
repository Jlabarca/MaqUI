// SPDX-License-Identifier: MIT
// MaqUI v2 — drag flag reads (V2-PLAYER-UI VPU.6).
//
// Covers the Node.OnDragStart/OnDragEnd accessor layer by setting the flags on
// InteractionState directly (same pattern as OnClick tests). The adapter's
// Down→Move→Up → DragStarted/DragEnded WIRING lives in
// UIToolkitInteractionAdapter, which the standalone csproj excludes as
// Unity-only; that half is validated live in play mode via the inventory
// drag-to-equip flow.

using System.Linq;
using Maqui.V2;
using Xunit;

namespace Maqui.V2.Tests
{
    public class DragFlagTests
    {
        [Fact]
        public void OnDragStart_TrueWhenFlagSet()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.DragStartedThisFrame);
            Assert.True(node.OnDragStart());
        }

        [Fact]
        public void OnDragEnd_TrueWhenFlagSet()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.DragEndedThisFrame);
            Assert.True(node.OnDragEnd());
        }

        [Fact]
        public void DragFlags_FalseByDefault()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            Assert.False(node.OnDragStart());
            Assert.False(node.OnDragEnd());
        }

        [Fact]
        public void OnDragEnd_RecordsAnInteractionOp()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            node.OnDragEnd();
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.OnDrag && o.NodeId == node.Id);
        }

        [Fact]
        public void DragFlags_ClearedByEndFrame()
        {
            // DragStarted/DragEnded are transient — a full frame boundary must wipe
            // them so a stale drag can't re-fire next frame.
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id,
                NodeInteractionFlags.DragStartedThisFrame | NodeInteractionFlags.DragEndedThisFrame);
            gui.EndFrame();

            Assert.False(gui.Interactions.Has(node.Id, NodeInteractionFlags.DragStartedThisFrame));
            Assert.False(gui.Interactions.Has(node.Id, NodeInteractionFlags.DragEndedThisFrame));
        }

        [Fact]
        public void NoneNode_DragReads_ShortCircuitFalse()
        {
            Assert.False(Node.None.OnDragStart());
            Assert.False(Node.None.OnDragEnd());
        }
    }
}
