// SPDX-License-Identifier: MIT
// MaqUI v2 — xUnit tests for PointerEventQueue + InteractionState + Node.* reads. P4.

using System.Collections.Generic;
using Xunit;

namespace Maqui.V2.Tests
{
    public class PointerEventQueueTests
    {
        [Fact]
        public void New_Queue_IsEmpty()
        {
            var q = new PointerEventQueue(8);
            Assert.True(q.IsEmpty);
            Assert.False(q.IsFull);
            Assert.Equal(0, q.Count);
            Assert.Equal(0, q.DroppedCount);
        }

        [Fact]
        public void Enqueue_Dequeue_FifoOrder()
        {
            var q = new PointerEventQueue(8);
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 1, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Move, 2, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Up,   3, "/", 0, 0));

            Assert.Equal(3, q.Count);
            Assert.True(q.TryDequeue(out var e1)); Assert.Equal(PointerEventKind.Down, e1.Kind); Assert.Equal(1, e1.TargetNodeId);
            Assert.True(q.TryDequeue(out var e2)); Assert.Equal(PointerEventKind.Move, e2.Kind); Assert.Equal(2, e2.TargetNodeId);
            Assert.True(q.TryDequeue(out var e3)); Assert.Equal(PointerEventKind.Up,   e3.Kind); Assert.Equal(3, e3.TargetNodeId);
            Assert.True(q.IsEmpty);
        }

        [Fact]
        public void TryDequeue_Empty_ReturnsFalse()
        {
            var q = new PointerEventQueue(4);
            Assert.False(q.TryDequeue(out _));
        }

        [Fact]
        public void Overflow_DropsOldest_AndCountsDrops()
        {
            var q = new PointerEventQueue(3);
            for (int i = 1; i <= 5; i++)
            {
                q.Enqueue(new PointerEvent(PointerEventKind.Move, i, "/", 0, 0));
            }
            Assert.Equal(3, q.Count);
            Assert.Equal(2, q.DroppedCount);

            // Remaining FIFO should be 3, 4, 5 (oldest two dropped).
            Assert.True(q.TryDequeue(out var e1)); Assert.Equal(3, e1.TargetNodeId);
            Assert.True(q.TryDequeue(out var e2)); Assert.Equal(4, e2.TargetNodeId);
            Assert.True(q.TryDequeue(out var e3)); Assert.Equal(5, e3.TargetNodeId);
        }

        [Fact]
        public void Drain_EmptiesQueue_InFifoOrder()
        {
            var q = new PointerEventQueue(8);
            for (int i = 1; i <= 4; i++)
                q.Enqueue(new PointerEvent(PointerEventKind.Move, i, "/", 0, 0));

            var seen = new List<int>();
            q.Drain(e => seen.Add(e.TargetNodeId));

            Assert.Equal(new[] { 1, 2, 3, 4 }, seen);
            Assert.True(q.IsEmpty);
        }

        [Fact]
        public void Clear_EmptiesWithoutDraining()
        {
            var q = new PointerEventQueue(4);
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 1, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 2, "/", 0, 0));
            q.Clear();
            Assert.True(q.IsEmpty);
            Assert.False(q.TryDequeue(out _));
        }

        [Fact]
        public void RingBuffer_WrapsAroundCorrectly()
        {
            var q = new PointerEventQueue(3);
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 1, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 2, "/", 0, 0));
            q.TryDequeue(out _); // dequeue 1, advances head
            q.TryDequeue(out _); // dequeue 2
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 3, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 4, "/", 0, 0));
            q.Enqueue(new PointerEvent(PointerEventKind.Down, 5, "/", 0, 0)); // wraps

            Assert.Equal(3, q.Count);
            Assert.Equal(0, q.DroppedCount);
            Assert.True(q.TryDequeue(out var e3)); Assert.Equal(3, e3.TargetNodeId);
            Assert.True(q.TryDequeue(out var e4)); Assert.Equal(4, e4.TargetNodeId);
            Assert.True(q.TryDequeue(out var e5)); Assert.Equal(5, e5.TargetNodeId);
        }
    }

    public class InteractionStateTests
    {
        [Fact]
        public void GetFlags_UnknownNode_ReturnsNone()
        {
            var s = new InteractionState();
            Assert.Equal(NodeInteractionFlags.None, s.GetFlags(42));
        }

        [Fact]
        public void SetFlags_ThenGetFlags_RoundTrips()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover | NodeInteractionFlags.Active);
            Assert.Equal(NodeInteractionFlags.Hover | NodeInteractionFlags.Active, s.GetFlags(1));
        }

        [Fact]
        public void SetFlags_None_RemovesEntry()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover);
            Assert.Equal(1, s.TrackedCount);
            s.SetFlags(1, NodeInteractionFlags.None);
            Assert.Equal(0, s.TrackedCount);
        }

        [Fact]
        public void AddFlags_OrsIntoExisting()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover);
            s.AddFlags(1, NodeInteractionFlags.Active);
            Assert.Equal(NodeInteractionFlags.Hover | NodeInteractionFlags.Active, s.GetFlags(1));
        }

        [Fact]
        public void ClearFlags_RemovesSpecificBits()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover | NodeInteractionFlags.Active);
            s.ClearFlags(1, NodeInteractionFlags.Hover);
            Assert.Equal(NodeInteractionFlags.Active, s.GetFlags(1));
        }

        [Fact]
        public void ClearFlags_AllBits_RemovesEntry()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover);
            s.ClearFlags(1, NodeInteractionFlags.Hover);
            Assert.Equal(0, s.TrackedCount);
        }

        [Fact]
        public void Has_RespectsCompositeFlag()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover);
            Assert.True(s.Has(1, NodeInteractionFlags.Hover));
            Assert.False(s.Has(1, NodeInteractionFlags.Active));
        }

        [Fact]
        public void Reset_ClearsAll()
        {
            var s = new InteractionState();
            s.SetFlags(1, NodeInteractionFlags.Hover);
            s.SetFlags(2, NodeInteractionFlags.Focus);
            s.Reset();
            Assert.Equal(0, s.TrackedCount);
        }
    }

    public class NodeInteractionReadTests
    {
        [Fact]
        public void IsHovered_ReturnsState()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Hover);
            Assert.True(node.IsHovered());
            Assert.False(node.IsActive());
            Assert.False(node.IsFocused());
            gui.EndFrame();
        }

        [Fact]
        public void IsActive_ReturnsState()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Active);
            Assert.True(node.IsActive());
            gui.EndFrame();
        }

        [Fact]
        public void IsFocused_ReturnsState()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Focus);
            Assert.True(node.IsFocused());
            gui.EndFrame();
        }

        [Fact]
        public void OnClick_TrueWhenClickedThisFrameSet()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.ClickedThisFrame);
            Assert.True(node.OnClick());
            gui.EndFrame();
        }

        [Fact]
        public void OnClick_FalseByDefault()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            Assert.False(node.OnClick());
            gui.EndFrame();
        }

        [Fact]
        public void OnHover_RecordsAndReadsState()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Hover);
            bool hovered = node.OnHover();
            gui.EndFrame();
            Assert.True(hovered);
            // OnHover also records — verify the FrameOp landed.
            int onHoverCount = 0;
            foreach (var op in gui.Buffer.Ops)
                if (op.Kind == FrameOpKind.OnHover && op.NodeId == node.Id) onHoverCount++;
            Assert.Equal(1, onHoverCount);
        }

        [Fact]
        public void OnHold_TrueWhenActive()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Active);
            Assert.True(node.OnHold());
            gui.EndFrame();
        }

        [Fact]
        public void OnDrag_TrueWhenActive()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Active);
            Assert.True(node.OnDrag());
            gui.EndFrame();
        }

        [Fact]
        public void None_NodeNoneInteractions_ShortCircuit()
        {
            Assert.False(Node.None.IsHovered());
            Assert.False(Node.None.IsActive());
            Assert.False(Node.None.IsFocused());
            Assert.False(Node.None.OnClick());
        }

        [Fact]
        public void BeginFrame_ResetsInteractionFlags()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var node = gui.Box();
            gui.Interactions.SetFlags(node.Id, NodeInteractionFlags.Hover);
            gui.EndFrame();

            // New frame — Reset should fire on BeginFrame.
            gui.BeginFrame();
            Assert.Equal(0, gui.Interactions.TrackedCount);
            gui.EndFrame();
        }
    }
}
