// SPDX-License-Identifier: MIT
// MaqUI v2 — Reconciler. Consumes Gui.Buffer; drives IBackend.

using System.Collections.Generic;

namespace Maqui.V2
{
    /// <summary>
    /// Diffs a frame buffer against the prior frame's element map and emits
    /// the minimal set of <see cref="IBackend"/> calls to bring the backend's
    /// element tree in line with the new frame.
    ///
    /// <para>Algorithm (O(n) keyed-map, not LCS):</para>
    /// <list type="number">
    ///   <item>Walk the buffer linearly. Maintain a parent stack and a
    ///   per-(scope, kind) ordinal counter.</item>
    ///   <item>For each <see cref="FrameOp"/> that maps to an element
    ///   (containers + leaves; not interactions or scope markers), derive
    ///   a <see cref="ReconcileKey"/>.</item>
    ///   <item>Hit in <c>_prev</c> → reuse handle, call <c>UpdateElement</c>.
    ///   Miss → call <c>CreateElement</c>.</item>
    ///   <item>Always call <c>SetParent(handle, parentTop, siblingIndex)</c>.</item>
    ///   <item>After the walk, recycle any prior-frame handle whose key
    ///   wasn't seen this frame.</item>
    /// </list>
    ///
    /// <para>Interaction ops (OnClick/OnHover/OnHold/OnDrag) are pass-through
    /// at v0 — they remain in the buffer but the reconciler doesn't surface
    /// them to the backend. Real interaction lives in P4.</para>
    /// </summary>
    internal sealed class Reconciler
    {
        private readonly Dictionary<ReconcileKey, int> _prev = new(capacity: 64);
        private readonly Dictionary<ReconcileKey, int> _current = new(capacity: 64);
        private readonly Dictionary<(string scope, FrameOpKind kind), int> _ordinalByScopeKind = new();
        private readonly Stack<int> _parentStack = new(capacity: 16);
        private readonly Stack<int> _siblingIndexStack = new(capacity: 16);

        /// <summary>Drive <paramref name="backend"/> from the ops in <paramref name="buffer"/>.</summary>
        public void Reconcile(FrameBuffer buffer, IBackend backend)
        {
            backend.BeginReconcile();

            _current.Clear();
            _ordinalByScopeKind.Clear();
            _parentStack.Clear();
            _siblingIndexStack.Clear();
            _parentStack.Push(backend.Root);
            _siblingIndexStack.Push(0);

            var ops = buffer.Ops;
            for (int i = 0; i < ops.Count; i++)
            {
                ProcessOp(ops, i, backend);
            }

            // Anything in _prev not in _current is stale — recycle.
            foreach (var kv in _prev)
            {
                if (!_current.ContainsKey(kv.Key))
                {
                    backend.Recycle(kv.Value);
                }
            }

            // Swap: this frame becomes the new prior.
            _prev.Clear();
            foreach (var kv in _current) _prev[kv.Key] = kv.Value;

            backend.EndReconcile();
        }

        private void ProcessOp(IReadOnlyList<FrameOp> ops, int i, IBackend backend)
        {
            var op = ops[i];
            switch (op.Kind)
            {
                case FrameOpKind.RowBegin:
                case FrameOpKind.ColumnBegin:
                case FrameOpKind.ClipBoxBegin:
                case FrameOpKind.Box:
                {
                    int handle = ReconcileLeaf(op, backend);
                    if (op.Kind == FrameOpKind.RowBegin
                        || op.Kind == FrameOpKind.ColumnBegin
                        || op.Kind == FrameOpKind.ClipBoxBegin)
                    {
                        // Push as new parent; reset sibling index counter for children.
                        _parentStack.Push(handle);
                        _siblingIndexStack.Push(0);
                    }
                    break;
                }

                case FrameOpKind.RowEnd:
                case FrameOpKind.ColumnEnd:
                case FrameOpKind.ClipBoxEnd:
                    if (_parentStack.Count > 1) // never pop the backend root
                    {
                        _parentStack.Pop();
                        _siblingIndexStack.Pop();
                    }
                    break;

                case FrameOpKind.Spacer:
                case FrameOpKind.DrawRect:
                case FrameOpKind.DrawText:
                case FrameOpKind.DrawLine:
                case FrameOpKind.DrawCircle:
                case FrameOpKind.DrawImage:
                case FrameOpKind.TextInputField:
                    ReconcileLeaf(op, backend);
                    break;

                case FrameOpKind.OnClick:
                case FrameOpKind.OnHover:
                case FrameOpKind.OnHold:
                case FrameOpKind.OnDrag:
                    // P4 territory — record-only at v0. No backend call.
                    break;

                case FrameOpKind.ScopeEnter:
                case FrameOpKind.ScopeExit:
                    // Scope-path is captured per-op in FrameOp.ScopePath; the
                    // reconciler doesn't need to track a stack for it.
                    break;
            }
        }

        private int ReconcileLeaf(in FrameOp op, IBackend backend)
        {
            // Ordinal-within-scope: how many ops of this kind have we seen in this scope this frame?
            var ordinalKey = (op.ScopePath, op.Kind);
            _ordinalByScopeKind.TryGetValue(ordinalKey, out int ordinal);
            _ordinalByScopeKind[ordinalKey] = ordinal + 1;

            var key = new ReconcileKey(op.ScopePath, op.Kind, ordinal);

            int handle;
            if (_prev.TryGetValue(key, out handle))
            {
                backend.UpdateElement(handle, in op);
            }
            else
            {
                handle = backend.CreateElement(in op);
            }
            _current[key] = handle;

            int parent = _parentStack.Peek();
            int siblingIndex = _siblingIndexStack.Peek();
            backend.SetParent(handle, parent, siblingIndex);
            // Increment sibling-index for next sibling under same parent.
            // Stack-of-ints + struct can't mutate top in place — pop+push.
            _siblingIndexStack.Pop();
            _siblingIndexStack.Push(siblingIndex + 1);

            return handle;
        }
    }
}
