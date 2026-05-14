// SPDX-License-Identifier: MIT
// MaqUI v2 — UIToolkitBackend. IBackend impl over UnityEngine.UIElements.
//
// =====================================================================
// MAQUIV2.2 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// This file references UnityEngine.UIElements (VisualElement, IStyle, etc.)
// which the dev box's standalone csproj does NOT shim. The file is shipped
// as-written from a Unity-less environment; the operator's first Unity
// open of MaqUI/CharqUI will compile-check it. Any compile errors should
// be fixed in-place before MAQUIV2.2.2 checkbox flips.
//
// Behavior expectations (covered by Maqui.V2.Tests.Runtime.ReconcilerParityTests
// in P2.6 — also a blind draft):
//   - CreateElement(op) mints a VisualElement of the right shape per op.Kind
//   - UpdateElement(handle, op) updates style props in place (no re-layout)
//   - SetParent reparents and reorders within the parent's children list
//   - Recycle detaches from parent + returns to per-kind pool
//   - BeginReconcile/EndReconcile are no-ops at v0 (no batched flush)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Maqui.V2
{
    /// <summary>
    /// <see cref="IBackend"/> implementation over UI Toolkit. One instance per
    /// UI root; constructed with a <see cref="VisualElement"/> root (typically
    /// <c>UIDocument.rootVisualElement</c>).
    ///
    /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
    /// </summary>
    public sealed class UIToolkitBackend : IBackend
    {
        private readonly VisualElement _root;
        private readonly Dictionary<int, VisualElement> _elements = new();
        private readonly Dictionary<FrameOpKind, Stack<VisualElement>> _pool = new();
        private const int MaxPoolSizePerKind = 64;
        private int _nextHandle = 1; // 0 reserved for Root.

        public UIToolkitBackend(VisualElement root)
        {
            _root = root ?? throw new System.ArgumentNullException(nameof(root));
            _elements[0] = root;
        }

        public int Root => 0;

        public void BeginReconcile() { /* no-op v0 */ }

        public void EndReconcile() { /* no-op v0; future: flush layout batch */ }

        public int CreateElement(in FrameOp op)
        {
            VisualElement element = TryPop(op.Kind) ?? Build(op.Kind);
            ApplyProps(element, in op);

            int handle = _nextHandle++;
            _elements[handle] = element;
            return handle;
        }

        public void UpdateElement(int handle, in FrameOp op)
        {
            if (!_elements.TryGetValue(handle, out var element)) return;
            ApplyProps(element, in op);
        }

        public void SetParent(int child, int parent, int siblingIndex)
        {
            if (!_elements.TryGetValue(child, out var childEl)) return;
            if (!_elements.TryGetValue(parent, out var parentEl)) return;

            if (childEl.parent != parentEl)
            {
                childEl.RemoveFromHierarchy();
                int clampedIndex = Mathf.Clamp(siblingIndex, 0, parentEl.childCount);
                parentEl.Insert(clampedIndex, childEl);
            }
            else if (parentEl.IndexOf(childEl) != siblingIndex)
            {
                int clampedIndex = Mathf.Clamp(siblingIndex, 0, parentEl.childCount - 1);
                childEl.PlaceInFront(parentEl[clampedIndex]);
            }
        }

        public void Recycle(int handle)
        {
            if (handle == Root) return; // never recycle root
            if (!_elements.TryGetValue(handle, out var element)) return;
            _elements.Remove(handle);
            element.RemoveFromHierarchy();
            // Pool by kind not tracked; approximate by element type. For v0 we
            // pool everything into a single "generic" bucket. Future work: track
            // kind-per-handle to pool more precisely.
            Push(FrameOpKind.Box, element);
        }

        // ---------- Internals ----------

        private VisualElement Build(FrameOpKind kind)
        {
            switch (kind)
            {
                case FrameOpKind.RowBegin:
                    return new VisualElement { style = { flexDirection = FlexDirection.Row } };
                case FrameOpKind.ColumnBegin:
                    return new VisualElement { style = { flexDirection = FlexDirection.Column } };
                case FrameOpKind.Box:
                case FrameOpKind.Spacer:
                case FrameOpKind.DrawRect:
                case FrameOpKind.DrawLine:
                case FrameOpKind.DrawCircle:
                    return new VisualElement();
                case FrameOpKind.DrawText:
                    return new Label();
                default:
                    return new VisualElement();
            }
        }

        private static void ApplyProps(VisualElement element, in FrameOp op)
        {
            switch (op.Kind)
            {
                case FrameOpKind.DrawText:
                    if (element is Label label)
                    {
                        label.text = op.Text ?? string.Empty;
                        label.style.color = new StyleColor(op.Color);
                        if (op.FloatA > 0f) label.style.fontSize = op.FloatA;
                    }
                    break;
                case FrameOpKind.DrawRect:
                case FrameOpKind.Box:
                    element.style.backgroundColor = new StyleColor(op.Color);
                    ApplySizeIfSet(element, in op);
                    break;
                case FrameOpKind.DrawCircle:
                    element.style.backgroundColor = new StyleColor(op.Color);
                    if (op.FloatA > 0f)
                    {
                        float d = op.FloatA * 2f;
                        element.style.width = d;
                        element.style.height = d;
                        element.style.borderTopLeftRadius = op.FloatA;
                        element.style.borderTopRightRadius = op.FloatA;
                        element.style.borderBottomLeftRadius = op.FloatA;
                        element.style.borderBottomRightRadius = op.FloatA;
                    }
                    break;
                case FrameOpKind.DrawLine:
                    element.style.backgroundColor = new StyleColor(op.Color);
                    if (op.FloatA > 0f) element.style.height = op.FloatA;
                    break;
                case FrameOpKind.RowBegin:
                case FrameOpKind.ColumnBegin:
                    ApplySizeIfSet(element, in op);
                    break;
                case FrameOpKind.Spacer:
                    ApplySizeIfSet(element, in op);
                    break;
            }
        }

        private static void ApplySizeIfSet(VisualElement element, in FrameOp op)
        {
            // Width: FloatA/B encode (kind, value); Height: FloatC/D.
            // For v0 we only honor Pixels — Expand/Fit/Ratio/Percentage layout
            // resolution lives in P2.x (post-v0 polish) or borrows from
            // UI Toolkit's own flex layout.
            SizeKind wKind = (SizeKind)(int)op.FloatA;
            SizeKind hKind = (SizeKind)(int)op.FloatC;
            if (wKind == SizeKind.Pixels && op.FloatB > 0f) element.style.width = op.FloatB;
            if (hKind == SizeKind.Pixels && op.FloatD > 0f) element.style.height = op.FloatD;
        }

        private VisualElement TryPop(FrameOpKind kind)
        {
            if (_pool.TryGetValue(kind, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
            return null;
        }

        private void Push(FrameOpKind kind, VisualElement element)
        {
            if (!_pool.TryGetValue(kind, out var stack))
            {
                stack = new Stack<VisualElement>();
                _pool[kind] = stack;
            }
            if (stack.Count < MaxPoolSizePerKind)
            {
                stack.Push(element);
            }
            // Otherwise let GC reclaim it.
        }
    }
}
