// SPDX-License-Identifier: MIT
// MaqUI v2 — Node handle. See docs/products/maqui-v2-spec.md.

using System;

namespace Maqui.V2
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
    /// All P1 implementations are <b>record-only</b> — they append a FrameOp and
    /// return <c>false</c>. Real interaction lives in P4 once the reconciler
    /// (P2) routes pointer events back into the frame buffer.
    /// </summary>
    public static class NodeInteractions
    {
        public static bool OnClick(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnClick, node);
            return false; // P1 record-only; P4 will return the real value.
        }

        public static bool OnHover(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnHover, node);
            return false;
        }

        public static bool OnHold(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnHold, node);
            return false;
        }

        public static bool OnDrag(this Node node)
        {
            if (node.IsNone) return false;
            node.Owner.RecordInteraction(FrameOpKind.OnDrag, node);
            return false;
        }
    }
}
