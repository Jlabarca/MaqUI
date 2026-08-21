// SPDX-License-Identifier: MIT
// MaqUI v2 — InteractionState. Per-NodeId interaction flag table.

using System;
using System.Collections.Generic;

namespace Maqui
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
        /// Reset all flags, including persistent ones (Hover/Active/Focus).
        /// Exposed for tests; NOT called per-frame by <see cref="Gui"/> — see
        /// <see cref="ClearTransientFlags"/> for why.
        /// </summary>
        public void Reset()
        {
            _flags.Clear();
        }

        private const NodeInteractionFlags TransientMask =
            NodeInteractionFlags.ClickedThisFrame
            | NodeInteractionFlags.DragStartedThisFrame
            | NodeInteractionFlags.DragEndedThisFrame;

        private static readonly List<int> s_ScratchKeys = new(capacity: 32);

        /// <summary>
        /// Clear only the one-shot per-frame pulse flags (ClickedThisFrame,
        /// DragStarted/EndedThisFrame). Called by <see cref="Gui.EndFrame"/>
        /// AFTER this frame's UI code has had a chance to read them.
        ///
        /// <para>Deliberately leaves Hover/Active/Focus untouched — those are
        /// real pointer state, toggled only by the adapter's Enter/Leave/
        /// Down/Up handlers, and must survive across frames. The real-world
        /// adapter (<c>UIToolkitInteractionAdapter</c>) is event-driven, not
        /// polling — it does NOT "re-emit" Hover/Active every frame the way
        /// an earlier design assumed. Unity runs Update() (where pointer
        /// events dispatch) before LateUpdate() (where Gui frames run), so a
        /// full <see cref="Reset"/> at frame-start would wipe a PointerDown's
        /// Active flag before the next frame's PointerUp could ever read
        /// <c>wasActive</c> — silently breaking every click. Confirmed via
        /// live diagnostic: Down/Up events fired correctly but clicks never
        /// registered, until this was split out.</para>
        /// </summary>
        public void ClearTransientFlags()
        {
            s_ScratchKeys.Clear();
            foreach (var kv in _flags)
            {
                if ((kv.Value & TransientMask) != NodeInteractionFlags.None)
                    s_ScratchKeys.Add(kv.Key);
            }
            for (int i = 0; i < s_ScratchKeys.Count; i++)
            {
                ClearFlags(s_ScratchKeys[i], TransientMask);
            }
        }
    }
}
