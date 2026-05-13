// SPDX-License-Identifier: MIT
// MaqUI v2 — FrameOp record. Internal; consumed by the P2 reconciler.

using UnityEngine;

namespace Maqui.V2
{
    /// <summary>
    /// Discriminates a <see cref="FrameOp"/>. Begin/End ops bracket containers so
    /// the linear FrameBuffer can be parsed back into a tree by the reconciler (P2).
    /// </summary>
    internal enum FrameOpKind : byte
    {
        // Layout containers (paired Begin/End):
        RowBegin = 1,
        RowEnd = 2,
        ColumnBegin = 3,
        ColumnEnd = 4,

        // Layout leaves:
        Box = 10,
        Spacer = 11,

        // Draw primitives:
        DrawRect = 20,
        DrawText = 21,
        DrawLine = 22,
        DrawCircle = 23,

        // Interaction (recorded against a parent Node):
        OnClick = 30,
        OnHover = 31,
        OnHold = 32,
        OnDrag = 33,

        // Data scope:
        ScopeEnter = 40,
        ScopeExit = 41,
    }

    /// <summary>
    /// One immediate-mode call captured during a frame. Layout, draw, interaction,
    /// and scope ops all share this struct via a tagged payload — keeps the frame
    /// buffer a single contiguous List with no boxing.
    ///
    /// <para>Internal because the P1 contract is "you call <see cref="Gui"/> methods,
    /// you get back a <see cref="Node"/>" — callers never touch FrameOp directly.
    /// Tests reach in via <c>internal</c> visibility (InternalsVisibleTo, set in 1.7).</para>
    /// </summary>
    internal readonly struct FrameOp
    {
        public FrameOpKind Kind { get; }
        public int NodeId { get; }

        /// <summary>Scope-path snapshot at emit time (joined ancestor keys).</summary>
        public string ScopePath { get; }

        /// <summary>Generic float payload (Size, alignment, etc.). Meaning depends on Kind.</summary>
        public float FloatA { get; }

        public float FloatB { get; }
        public float FloatC { get; }
        public float FloatD { get; }

        /// <summary>Color payload (used by DrawRect/DrawText/DrawLine/DrawCircle).</summary>
        public Color32 Color { get; }

        /// <summary>String payload (used by DrawText, ScopeEnter/Exit key).</summary>
        public string Text { get; }

        public FrameOp(
            FrameOpKind kind,
            int nodeId,
            string scopePath,
            float a = 0f, float b = 0f, float c = 0f, float d = 0f,
            Color32 color = default,
            string text = null)
        {
            Kind = kind;
            NodeId = nodeId;
            ScopePath = scopePath;
            FloatA = a;
            FloatB = b;
            FloatC = c;
            FloatD = d;
            Color = color;
            Text = text;
        }

        public override string ToString()
        {
            string path = ScopePath ?? "/";
            return $"[{Kind} #{NodeId} @{path}]";
        }
    }
}
