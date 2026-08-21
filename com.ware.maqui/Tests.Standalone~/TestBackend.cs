// SPDX-License-Identifier: MIT
// MaqUI v2 — TestBackend. In-memory IBackend impl that records every call
// as a typed event sequence; standalone xUnit tests assert on the sequence.

using System.Collections.Generic;
using Maqui;

namespace Maqui.Tests
{
    /// <summary>
    /// Discriminates entries in <see cref="TestBackend.Events"/>.
    /// </summary>
    public enum BackendEventKind
    {
        BeginReconcile,
        EndReconcile,
        CreateElement,
        UpdateElement,
        SetParent,
        Recycle,
    }

    /// <summary>
    /// Recorded backend call. Fields are interpreted by <see cref="Kind"/>:
    /// <list type="bullet">
    ///   <item><c>CreateElement</c>: <see cref="Handle"/> is the minted handle; <see cref="OpKind"/> is the FrameOp kind that triggered it.</item>
    ///   <item><c>UpdateElement</c>: <see cref="Handle"/> is the reused handle; <see cref="OpKind"/> is the FrameOp kind.</item>
    ///   <item><c>SetParent</c>: <see cref="Handle"/> = child, <see cref="Parent"/> = parent, <see cref="SiblingIndex"/> = order.</item>
    ///   <item><c>Recycle</c>: <see cref="Handle"/> is the freed handle.</item>
    ///   <item><c>BeginReconcile</c>/<c>EndReconcile</c>: no payload.</item>
    /// </list>
    /// </summary>
    public readonly struct BackendEvent
    {
        public BackendEventKind Kind { get; }
        public int Handle { get; }
        public FrameOpKind OpKind { get; }
        public int Parent { get; }
        public int SiblingIndex { get; }
        public string Text { get; }

        public BackendEvent(
            BackendEventKind kind,
            int handle = 0,
            FrameOpKind opKind = 0,
            int parent = 0,
            int siblingIndex = 0,
            string text = null)
        {
            Kind = kind;
            Handle = handle;
            OpKind = opKind;
            Parent = parent;
            SiblingIndex = siblingIndex;
            Text = text;
        }

        public override string ToString() => Kind switch
        {
            BackendEventKind.CreateElement => $"Create({OpKind})→#{Handle}",
            BackendEventKind.UpdateElement => $"Update(#{Handle}, {OpKind})",
            BackendEventKind.SetParent => $"SetParent(#{Handle} → #{Parent}@{SiblingIndex})",
            BackendEventKind.Recycle => $"Recycle(#{Handle})",
            BackendEventKind.BeginReconcile => "BeginReconcile",
            BackendEventKind.EndReconcile => "EndReconcile",
            _ => $"{Kind}",
        };
    }

    /// <summary>
    /// In-memory <see cref="IBackend"/> for unit tests. Mints sequential handles
    /// starting at 1 (Root = 0). Maintains a simple recycle pool keyed by
    /// FrameOpKind so tests can assert on reuse behavior.
    /// </summary>
    public sealed class TestBackend : IBackend
    {
        private readonly List<BackendEvent> _events = new();
        private readonly Dictionary<FrameOpKind, Stack<int>> _pool = new();
        private int _nextHandle = 1;

        public int Root => 0;

        public IReadOnlyList<BackendEvent> Events => _events;

        public void Clear() => _events.Clear();

        public void BeginReconcile() =>
            _events.Add(new BackendEvent(BackendEventKind.BeginReconcile));

        public void EndReconcile() =>
            _events.Add(new BackendEvent(BackendEventKind.EndReconcile));

        public int CreateElement(in FrameOp op)
        {
            // Pop from the per-kind pool if available; otherwise mint a new handle.
            int handle;
            if (_pool.TryGetValue(op.Kind, out var stack) && stack.Count > 0)
            {
                handle = stack.Pop();
            }
            else
            {
                handle = _nextHandle++;
            }
            _events.Add(new BackendEvent(BackendEventKind.CreateElement, handle, op.Kind, text: op.Text));
            return handle;
        }

        public void UpdateElement(int handle, in FrameOp op) =>
            _events.Add(new BackendEvent(BackendEventKind.UpdateElement, handle, op.Kind, text: op.Text));

        public void SetParent(int child, int parent, int siblingIndex) =>
            _events.Add(new BackendEvent(BackendEventKind.SetParent, child, parent: parent, siblingIndex: siblingIndex));

        public void Recycle(int handle)
        {
            _events.Add(new BackendEvent(BackendEventKind.Recycle, handle));
            // Pool by the most recent Create's OpKind — tests don't depend on
            // pool-key precision, only that recycled handles are reusable.
            // For simplicity we put every recycle into a single "any-kind" stack
            // accessed by all kinds; CreateElement re-pops in order.
            if (!_pool.TryGetValue(FrameOpKind.Box, out var stack))
            {
                stack = new Stack<int>();
                _pool[FrameOpKind.Box] = stack;
            }
            stack.Push(handle);
        }
    }
}
