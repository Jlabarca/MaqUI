// SPDX-License-Identifier: MIT
// MaqUI v2 — FrameBuffer. Internal; per-Gui instance.

using System.Collections.Generic;

namespace Maqui
{
    /// <summary>
    /// Ordered list of <see cref="FrameOp"/>s recorded during a frame. The P2
    /// reconciler will consume this and diff against the prior frame to patch
    /// the UI Toolkit VisualElement tree. P1 just records.
    /// </summary>
    internal sealed class FrameBuffer
    {
        private readonly List<FrameOp> _ops = new List<FrameOp>(capacity: 256);

        public int Count => _ops.Count;

        /// <summary>Read-only view for the P2 reconciler and for tests.</summary>
        public IReadOnlyList<FrameOp> Ops => _ops;

        public void Record(in FrameOp op) => _ops.Add(op);

        public void Clear() => _ops.Clear();
    }
}
