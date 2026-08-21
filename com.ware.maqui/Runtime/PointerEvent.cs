// SPDX-License-Identifier: MIT
// MaqUI v2 — PointerEvent record + kind enum.

namespace Maqui
{
    /// <summary>
    /// Discriminates a <see cref="PointerEvent"/>. Maps roughly 1:1 to UI Toolkit
    /// pointer event types — adapter (Unity-side) translates them.
    /// </summary>
    public enum PointerEventKind : byte
    {
        Down = 1,   // primary button pressed inside element
        Up = 2,     // primary button released anywhere; TargetNodeId is where Down originated
        Move = 3,   // pointer moved while inside element
        Enter = 4,  // pointer entered element
        Leave = 5,  // pointer left element
    }

    /// <summary>
    /// One pointer event, captured by the Unity-side adapter and queued for
    /// drain at the next frame boundary.
    ///
    /// <para><c>TargetNodeId</c> is the per-frame Node.Id of the element the
    /// adapter resolved as the hit target. <c>TargetScopePath</c> is captured
    /// at hit-resolution time. Together they're the (non-stable across frames)
    /// addressing the receiver uses.</para>
    /// </summary>
    public readonly struct PointerEvent
    {
        public PointerEventKind Kind { get; }
        public int TargetNodeId { get; }
        public string TargetScopePath { get; }
        public float X { get; }
        public float Y { get; }
        public int Button { get; }
        public float Timestamp { get; }

        public PointerEvent(
            PointerEventKind kind,
            int targetNodeId,
            string targetScopePath,
            float x,
            float y,
            int button = 0,
            float timestamp = 0f)
        {
            Kind = kind;
            TargetNodeId = targetNodeId;
            TargetScopePath = targetScopePath ?? "/";
            X = x;
            Y = y;
            Button = button;
            Timestamp = timestamp;
        }

        public override string ToString() =>
            $"[{Kind} node#{TargetNodeId} @{TargetScopePath} ({X},{Y}) btn{Button}]";
    }
}
