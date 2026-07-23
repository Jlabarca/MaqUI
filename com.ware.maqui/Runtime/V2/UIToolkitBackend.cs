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

        /// <summary>
        /// Wires pointer events into the backend's created elements. Must be
        /// assigned before the first reconcile if callers want interaction
        /// (Hover/Active/ClickedThisFrame) to work — without it, elements are
        /// created/updated but no <see cref="UIToolkitInteractionAdapter"/>
        /// callback is ever registered on them, so <c>Node.OnClick()</c> etc.
        /// never fire. Settable after construction because the adapter's own
        /// constructor takes this backend (construction-order chicken/egg).
        /// </summary>
        public UIToolkitInteractionAdapter Adapter { get; set; }

        public int Root => 0;

        public void BeginReconcile() { /* no-op v0 */ }

        public void EndReconcile() { /* no-op v0; future: flush layout batch */ }

        public int CreateElement(in FrameOp op)
        {
            VisualElement element = TryPop(op.Kind) ?? Build(op.Kind);
            ApplyProps(element, in op);

            int handle = _nextHandle++;
            _elements[handle] = element;

            if (Adapter != null)
            {
                Adapter.NoteHandleFrameOp(handle, in op);
                Adapter.SubscribeIfNew(handle, element);
            }

            return handle;
        }

        public void UpdateElement(int handle, in FrameOp op)
        {
            if (!_elements.TryGetValue(handle, out var element)) return;
            ApplyProps(element, in op);

            if (Adapter != null)
            {
                Adapter.NoteHandleFrameOp(handle, in op);
                Adapter.SubscribeIfNew(handle, element);
            }
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
            // Drop our class bookkeeping AND the class itself: a pooled element must
            // come back clean, and leaving the entry would grow the map forever with
            // elements that were dropped on the floor below.
            ApplyClassName(element, null);
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
            // ScrollView's contentContainer differs from a plain Box's — pooling it
            // back under FrameOpKind.Box would hand a stale scroll rig (scrollers,
            // content viewport) to a caller expecting a flat leaf VisualElement.
            if (element is ScrollView) return;
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
                case FrameOpKind.ScrollBegin:
                    // Real UI Toolkit ScrollView — native wheel + drag-scrollbar,
                    // no contentHeight math needed. Insert()/Add()/childCount all
                    // route through its overridden contentContainer automatically,
                    // so SetParent/Recycle below need no ScrollView-specific case.
                    return new ScrollView(ScrollViewMode.Vertical);
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
            ApplyClassName(element, op.ClassName);

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
                    // v0 default chrome: every filled rect gets a small corner
                    // radius so stock components (Button, Toggle pill, ...)
                    // don't read as bare UGUI-default flat rectangles.
                    ApplyCornerRadius(element, Maqui.V2.Components.MaquiTheme.CornerRadius);
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
                case FrameOpKind.ScrollBegin:
                    ApplySizeIfSet(element, in op);
                    ApplyAlignItems(element, op.AlignItems);
                    if (op.MaxHeight > 0f) element.style.maxHeight = op.MaxHeight;
                    // Container chrome: Color.a > 0 opts a container into a real
                    // painted background + fixed padding, instead of callers
                    // faking it with a sibling DrawRect (which can't sit "behind"
                    // the container's own children in a flex layout).
                    if (op.Color.a > 0)
                    {
                        element.style.backgroundColor = new StyleColor(op.Color);
                        // Horizontal-only: a background-carrying container can be as short as a
                        // 32px button, and top+bottom padding would eat most of that height.
                        // Callers wanting vertical breathing room add their own Spacer().
                        element.style.paddingLeft = Maqui.V2.Components.MaquiTheme.ContainerPadding;
                        element.style.paddingRight = Maqui.V2.Components.MaquiTheme.ContainerPadding;
                        ApplyCornerRadius(element, Maqui.V2.Components.MaquiTheme.PanelCornerRadius);
                    }
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

        /// <summary>Last USS class this backend put on each element. Elements are
        /// pooled and reused across frames, so applying a class without removing the
        /// previous one would let a recycled node keep styling from whatever it was
        /// two frames ago. Tracked here rather than read back off the element so we
        /// only ever touch classes we own — anything the host added stays untouched.
        /// </summary>
        private readonly Dictionary<VisualElement, string> _appliedClasses = new();

        /// <summary>Forwards <see cref="FrameOp.ClassName"/> to USS (WINDOW-LOOP
        /// WL.2.2). This is the seam that lets appearance live in a stylesheet
        /// instead of in draw-op colour arguments — delegating theming to UI Toolkit
        /// rather than growing a second theme system inside Maqui.</summary>
        private void ApplyClassName(VisualElement element, string className)
        {
            _appliedClasses.TryGetValue(element, out var previous);
            if (previous == className) return;

            if (!string.IsNullOrEmpty(previous)) element.RemoveFromClassList(previous);

            if (string.IsNullOrEmpty(className))
            {
                _appliedClasses.Remove(element);
                return;
            }

            element.AddToClassList(className);
            _appliedClasses[element] = className;
        }

        // Maqui.V2.AlignItems -> UnityEngine.UIElements.Align. The Unity enum is
        // fully qualified: this file is in namespace Maqui.V2 AND has a
        // `using UnityEngine.UIElements`, and Maqui.V2 declares its own `Align`
        // (the spec's float-constant class), so a bare `Align` here binds to the
        // Maqui one and would not compile.
        private static void ApplyAlignItems(VisualElement element, AlignItems align)
        {
            element.style.alignItems = align switch
            {
                AlignItems.Start => UnityEngine.UIElements.Align.FlexStart,
                AlignItems.Center => UnityEngine.UIElements.Align.Center,
                AlignItems.End => UnityEngine.UIElements.Align.FlexEnd,
                _ => UnityEngine.UIElements.Align.Stretch,
            };
        }

        private static void ApplyCornerRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
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
            // Expand: grow to fill remaining space along the parent's main axis —
            // e.g. a Spacer(Size.Expand()) between a title and a close button
            // pushes the button to the far edge of a Row. Same underlying Yoga
            // property regardless of which axis (width/height) carried the Expand
            // kind, so either slot maps to flexGrow.
            if (wKind == SizeKind.Expand) element.style.flexGrow = op.FloatB > 0f ? op.FloatB : 1f;
            if (hKind == SizeKind.Expand) element.style.flexGrow = op.FloatD > 0f ? op.FloatD : 1f;
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
