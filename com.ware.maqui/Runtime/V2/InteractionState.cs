// SPDX-License-Identifier: MIT
// MaqUI v2 — InteractionState. Per-NodeId interaction flag table.

using System;
using System.Collections.Generic;

namespace Maqui.V2
{
    /// <summary>
    /// Per-node interaction flags. Multiple bits can be set simultaneously
    /// (a node can be hovered AND active AND clicked-this-frame).
    /// </summary>
    [Flags]
    public enum NodeInteractionFlags : byte
    {
        None = 0,
        Hover = 1 << 0,
        Active = 1 << 1,           // pointer currently down on this node
        Focus = 1 << 2,            // keyboard focus
        ClickedThisFrame = 1 << 3, // primary click released over this node this frame
        DragStartedThisFrame = 1 << 4,
        DragEndedThisFrame = 1 << 5,
    }

    /// <summary>
    /// Per-frame interaction flag table keyed by <see cref="Node.Id"/>.
    /// Written by the Unity-side adapter (or directly by tests); read by
    /// <see cref="Node"/>'s interaction extension methods (<c>OnClick</c>,
    /// <c>IsHovered</c>, etc.).
    ///
    /// <para><b>Same-frame semantics:</b> NodeId is per-frame; the adapter
    /// must look up the current frame's NodeId for each incoming pointer
    /// event before calling <see cref="SetFlags"/>. Across-frame stickiness
    /// (e.g., "still hovered") is achieved by the adapter re-emitting flags
    /// each frame from sticky pointer position, not by InteractionState
    /// remembering. <see cref="Reset"/> at frame boundary enforces this.</para>
    /// </summary>
    public sealed class InteractionState
    {
        private readonly Dictionary<int, NodeInteractionFlags> _flags = new(capacity: 32);

        /// <summary>Replace flags for a node (overwrites any prior).</summary>
        public void SetFlags(int nodeId, NodeInteractionFlags flags)
        {
            if (flags == NodeInteractionFlags.None)
            {
                _flags.Remove(nodeId);
            }
            else
            {
                _flags[nodeId] = flags;
            }
        }

        /// <summary>Add flags onto a node's existing flag set.</summary>
        public void AddFlags(int nodeId, NodeInteractionFlags flags)
        {
            if (flags == NodeInteractionFlags.None) return;
            _flags.TryGetValue(nodeId, out var existing);
            _flags[nodeId] = existing | flags;
        }

        /// <summary>Remove specific flags from a node's flag set.</summary>
        public void ClearFlags(int nodeId, NodeInteractionFlags flags)
        {
            if (!_flags.TryGetValue(nodeId, out var existing)) return;
            var updated = existing & ~flags;
            if (updated == NodeInteractionFlags.None) _flags.Remove(nodeId);
            else _flags[nodeId] = updated;
        }

        public NodeInteractionFlags GetFlags(int nodeId)
        {
            return _flags.TryGetValue(nodeId, out var f) ? f : NodeInteractionFlags.None;
        }

        public bool Has(int nodeId, NodeInteractionFlags flag) =>
            (GetFlags(nodeId) & flag) == flag;

        /// <summary>Total number of nodes with non-empty flags.</summary>
        public int TrackedCount => _flags.Count;

        /// <summary>
        /// Reset all flags. Called by <see cref="Gui.BeginFrame"/> after the
        /// previous frame's pointer queue has been drained — so per-frame
        /// flags (Click, DragStart, DragEnd) don't carry forward, and the
        /// adapter freshly re-emits Hover/Active/Focus on each frame.
        /// </summary>
        public void Reset()
        {
            _flags.Clear();
        }
    }
}
