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

        private readonly Gui _gui;
        private readonly Maqui.V2.Components.IImageLoader _imageLoader;

        public UIToolkitBackend(VisualElement root, Gui gui = null, Maqui.V2.Components.IImageLoader imageLoader = null)
        {
            _root = root ?? throw new System.ArgumentNullException(nameof(root));
            _elements[0] = root;
            _gui = gui;
            _imageLoader = imageLoader;
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
            // Pool by element type. Specialized elements (Label/TextField) don't
            // pool back into the generic Box bucket — they'd be type-incompatible
            // when popped for a different FrameOpKind. Drop them on the floor;
            // GC reclaims.
            if (element is TextField tf)
            {
                _textFieldKeys.Remove(tf);
                return;
            }
            if (element is Label) return;
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
                case FrameOpKind.ClipBoxBegin:
                {
                    var el = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                    el.style.overflow = Overflow.Hidden;
                    return el;
                }
                case FrameOpKind.Box:
                case FrameOpKind.Spacer:
                case FrameOpKind.DrawRect:
                case FrameOpKind.DrawLine:
                case FrameOpKind.DrawCircle:
                case FrameOpKind.DrawImage:
                    return new VisualElement();
                case FrameOpKind.DrawText:
                    return new Label();
                case FrameOpKind.TextInputField:
                    return new TextField();
                default:
                    return new VisualElement();
            }
        }

        private void ApplyProps(VisualElement element, in FrameOp op)
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
                case FrameOpKind.DrawImage:
                    ApplySizeIfSet(element, in op);
                    if (_imageLoader != null && !string.IsNullOrEmpty(op.Text))
                    {
                        var tex = _imageLoader.Resolve(op.Text);
                        if (tex != null)
                        {
                            element.style.backgroundImage = new StyleBackground(tex);
                        }
                    }
                    break;
                case FrameOpKind.TextInputField:
                    ApplySizeIfSet(element, in op);
                    if (element is TextField tf)
                    {
                        ApplyTextFieldProps(tf, op.Text);
                    }
                    break;
                case FrameOpKind.RowBegin:
                case FrameOpKind.ColumnBegin:
                case FrameOpKind.ClipBoxBegin:
                    ApplySizeIfSet(element, in op);
                    break;
                case FrameOpKind.Spacer:
                    ApplySizeIfSet(element, in op);
                    break;
            }
        }

        private void ApplyTextFieldProps(TextField tf, string payload)
        {
            // Payload encoding: "<storeKey>|<initialValue>". Backend extracts the
            // store key and routes value-changed callbacks to gui.TextInputs.Set(key, ...).
            string storeKey = null;
            string initial = string.Empty;
            if (!string.IsNullOrEmpty(payload))
            {
                int sep = payload.IndexOf('|');
                if (sep >= 0)
                {
                    storeKey = payload.Substring(0, sep);
                    initial = payload.Substring(sep + 1);
                }
                else
                {
                    initial = payload;
                }
            }

            // First-time bind: write initial value into the store + subscribe.
            if (!_textFieldKeys.Contains(tf))
            {
                _textFieldKeys.Add(tf);
                if (_gui != null && storeKey != null && !_gui.TextInputs.Has(storeKey))
                {
                    _gui.TextInputs.Set(storeKey, initial);
                }
                tf.SetValueWithoutNotify(_gui != null && storeKey != null
                    ? _gui.TextInputs.Get(storeKey, initial)
                    : initial);
                if (_gui != null && storeKey != null)
                {
                    var localKey = storeKey;
                    tf.RegisterValueChangedCallback(evt => _gui.TextInputs.Set(localKey, evt.newValue));
                }
            }
            else if (_gui != null && storeKey != null)
            {
                // Reuse path (e.g., pool restore): re-sync from store without firing the callback.
                var cur = _gui.TextInputs.Get(storeKey, initial);
                if (tf.value != cur) tf.SetValueWithoutNotify(cur);
            }
        }

        private readonly HashSet<TextField> _textFieldKeys = new();

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
