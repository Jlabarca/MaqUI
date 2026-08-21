// SPDX-License-Identifier: MIT
// MaqUI v2 — Node handle. See docs/products/maqui-v2-spec.md.

using System;

namespace Maqui
{
    /// <summary>
    /// Cheap value handle returned by every layout primitive. Identifies the
    /// FrameOp this call recorded into the active <see cref="Gui"/>'s
    /// <see cref="FrameBuffer"/>. Safe to discard (`gui.Row()` without using
    /// the return value is a valid pattern).
    ///
    /// <para><c>default(Node)</c> is the null sentinel — <see cref="OpIndex"/>
    /// is <c>-1</c> and interaction extension methods short-circuit.</para>
    /// </summary>
    public readonly struct Node : IEquatable<Node>
    {
        /// <summary>Stable per-frame identifier (monotonic within a frame).</summary>
        public int Id { get; }

        /// <summary>Index into the owning <see cref="Gui"/>'s frame buffer.</summary>
        public int OpIndex { get; }

        /// <summary>Owning Gui — captured so interaction extension methods can
        /// emit follow-up ops without the caller threading the Gui through.</summary>
        internal Gui Owner { get; }

        internal Node(Gui owner, int id, int opIndex)
        {
            Owner = owner;
            Id = id;
            OpIndex = opIndex;
        }

        /// <summary>The default <c>Node</c> (no owner, OpIndex == -1). Interaction
        /// methods treat this as a no-op so accidental discard never crashes.</summary>
        public static Node None => default;

        public bool IsNone => Owner == null;

        public bool Equals(Node other) => Id == other.Id && OpIndex == other.OpIndex && ReferenceEquals(Owner, other.Owner);
        public override bool Equals(object obj) => obj is Node n && Equals(n);
        public override int GetHashCode() => unchecked(Id * 397 ^ OpIndex);
        public static bool operator ==(Node a, Node b) => a.Equals(b);
        public static bool operator !=(Node a, Node b) => !a.Equals(b);
    }

    /// <summary>
    /// Interaction primitives are extension methods on <see cref="Node"/> so
    /// callers can write <c>if (gui.Box(...).OnClick()) { ... }</c> per the spec.
    ///
    /// <para>As of P4, each method records the FrameOp AND reads the current
    /// flag from <see cref="Gui.Interactions"/> (written by the Unity-side
    /// adapter or by tests). Same-frame NodeId semantics — see
    /// <see cref="InteractionState"/> docs.</para>
    /// </summary>
    public static class NodeInteractions
    {
        public static bool OnClick(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnClick, node);
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.ClickedThisFrame);
        }

        public static bool OnHover(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnHover, node);
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Hover);
        }

        public static bool OnHold(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnHold, node);
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Active);
        }

        public static bool OnDrag(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnDrag, node);
            // "Dragging" = pointer is down (Active) AND has moved since down.
            // The adapter writes Active on PointerDown and DragStartedThisFrame
            // on the first Move after Down; we report dragging while Active.
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Active);
        }

        /// <summary>VPU.6. True on the frame a drag BEGAN on this node (pointer
        /// pressed here, then moved). Use on a drag source to snapshot its payload.</summary>
        public static bool OnDragStart(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnDrag, node);
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.DragStartedThisFrame);
        }

        /// <summary>VPU.6. True on the frame an in-progress drag was RELEASED over
        /// this node (the drop target — may differ from the source). Use on a drop
        /// zone to consume the drag payload and act on it.</summary>
        public static bool OnDragEnd(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnDrag, node);
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.DragEndedThisFrame);
        }

        // --- P4: readable flags inside immediate-mode call (4.3) ---

        /// <summary>P4. True if the pointer is over this node THIS FRAME.</summary>
        public static bool IsHovered(this Node node)
        {
            if (node.IsNone) return false;
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Hover);
        }

        /// <summary>P4. True if a pointer button is currently down on this node.</summary>
        public static bool IsActive(this Node node)
        {
            if (node.IsNone) return false;
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Active);
        }

        /// <summary>P4. True if this node has keyboard focus.</summary>
        public static bool IsFocused(this Node node)
        {
            if (node.IsNone) return false;
            return node.Owner.Interactions.Has(node.Id, NodeInteractionFlags.Focus);
        }
    }
}
