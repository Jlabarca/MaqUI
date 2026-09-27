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
// Behavior expectations (covered by Maqui.Tests.Runtime.ReconcilerParityTests
// in P2.6 — also a blind draft):
//   - CreateElement(op) mints a VisualElement of the right shape per op.Kind
//   - UpdateElement(handle, op) updates style props in place (no re-layout)
//   - SetParent reparents and reorders within the parent's children list
//   - Recycle detaches from parent + returns to per-kind pool
//   - BeginReconcile/EndReconcile are no-ops at v0 (no batched flush)

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Maqui
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

        /// <summary>
        /// FRAME-BUDGET.2.5: last *requested* <see cref="FrameOp"/> applied to each element,
        /// so <see cref="ApplyProps"/> can skip unchanged style writes. Compared against the
        /// requested op, never against <c>element.resolvedStyle</c> (GUARD.4 — resolvedStyle
        /// lags a frame behind a just-applied inline write and would misreport "unchanged").
        /// Cleared on rent (<see cref="CreateElement"/>'s pool pop) and on <see cref="Recycle"/>
        /// so a reused element always re-applies its first op in full.
        /// </summary>
        private readonly Dictionary<VisualElement, FrameOp> _lastProps = new();

        private readonly Gui _gui;
        private readonly Maqui.Components.IImageLoader _imageLoader;
        private readonly Maqui.Components.ISpriteLoader _spriteLoader;

        public UIToolkitBackend(VisualElement root, Gui gui = null,
            Maqui.Components.IImageLoader imageLoader = null,
            Maqui.Components.ISpriteLoader spriteLoader = null)
        {
            _root = root ?? throw new System.ArgumentNullException(nameof(root));
            _elements[0] = root;
            _gui = gui;
            _imageLoader = imageLoader;
            _spriteLoader = spriteLoader;
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
            VisualElement element = TryPop(op.Kind);
            if (element != null)
            {
                ResetPooledStyles(element);
                // FRAME-BUDGET.2.5: a rented element's actual style was just reset to null,
                // so any stale "last requested" entry from its PREVIOUS tenant must not be
                // trusted for the diff below.
                _lastProps.Remove(element);
            }
            else element = Build(op.Kind);
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
            // FRAME-BUDGET.2.5: whether this element is about to be pooled (Box bucket) or
            // dropped on the floor (Label/TextField/ScrollView below), its "last requested"
            // entry must not survive to mislead a future tenant's diff.
            _lastProps.Remove(element);
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
                // WL.0: native value-emitting controls. UI Toolkit owns the drag /
                // menu interaction; Maqui only records the desired value and reads
                // back what the user did.
                case FrameOpKind.SliderField:
                    return new Slider();
                case FrameOpKind.DropdownField:
                    return new DropdownField();
                default:
                    return new VisualElement();
            }
        }

        /// <summary>
        /// Whether an op's element takes part in hit-testing. Draw leaves do NOT:
        /// they are decorative by construction (the framework's interactive unit is
        /// always a CONTAINER — <c>Button</c>, <c>ItemSlot</c>, <c>EquipSlot</c> and
        /// <c>Hotbar</c> are all clickable <c>Column</c>s wrapping an icon and a
        /// label), and no caller anywhere in this package or in ORO captures a
        /// draw-leaf <see cref="Node"/> to query interaction on it.
        ///
        /// <para><b>Why this matters (the bug it fixes).</b> UI Toolkit defaults every
        /// element to <c>PickingMode.Position</c>, and nothing here used to override
        /// it. A press over an <c>ItemSlot</c>'s icon therefore had the ICON as its
        /// direct target — and <see cref="UIToolkitInteractionAdapter"/> arms the drag
        /// source only for the direct target (deliberately: without that gate every
        /// ancestor overwrote the source with the outermost container). So the slot
        /// never armed and <c>OnDragStart()</c> never fired. The only region that
        /// worked was the few pixels of slot rim the icon did not cover, which is
        /// exactly the reported symptom: "drag only starts from a very specific small
        /// point". Clicks were unaffected — Hover/Active/Clicked flag every node in
        /// the bubble chain — which is why every parity checklist passed this.</para>
        ///
        /// <para>Applied from <see cref="ApplyProps"/> rather than <see cref="Build"/>
        /// because elements are POOLED: <see cref="Recycle"/> pushes a plain
        /// <c>VisualElement</c> back under the <c>Box</c> bucket regardless of the op
        /// it was built for, so a former DrawImage can be popped as a container. Keying
        /// off the CURRENT op every frame is the only pooling-proof placement.</para>
        ///
        /// <para>Need a clickable image? Wrap it in a <c>Box</c>/<c>Column</c> and read
        /// interaction from that — the same idiom every existing component uses.</para>
        /// </summary>
        private static PickingMode PickingFor(FrameOpKind kind)
        {
            switch (kind)
            {
                case FrameOpKind.DrawText:
                case FrameOpKind.DrawImage:
                case FrameOpKind.DrawRect:
                case FrameOpKind.DrawLine:
                case FrameOpKind.DrawCircle:
                case FrameOpKind.Spacer:
                    return PickingMode.Ignore;
                default:
                    return PickingMode.Position;
            }
        }

        private void ApplyProps(VisualElement element, in FrameOp op)
        {
            // FRAME-BUDGET.2.5: skip the whole write path (including the ApplyClassName/
            // picking-mode guards below, which are already cheap but still branch) when
            // this op is identical, field for field, to the last one requested for this
            // element. Lever: Maqui.FrameBudgetFlags.PropDiff (ORO's /fb maquidiff).
            if (Maqui.FrameBudgetFlags.PropDiff
                && _lastProps.TryGetValue(element, out var lastOp)
                && lastOp.PropsEqual(in op))
            {
                return;
            }
            _lastProps[element] = op;

            ApplyClassName(element, op.ClassName);

            // Cheap guard rather than an unconditional write: this runs for every
            // element every frame, and pickingMode is a plain field set that would
            // otherwise churn needlessly.
            var picking = PickingFor(op.Kind);
            if (element.pickingMode != picking) element.pickingMode = picking;

            switch (op.Kind)
            {
                case FrameOpKind.DrawText:
                    if (element is Label label)
                    {
                        label.text = op.Text ?? string.Empty;
                        // Alpha 0 means "unset", not "invisible" — fully transparent
                        // text is never intentional, exactly as alpha 0 already means
                        // "no background" for containers below. Skipping the write is
                        // what lets a USS class own the colour: an INLINE style always
                        // beats a stylesheet in UI Toolkit, so writing it here would
                        // make the class unable to affect colour at all.
                        if (op.Color.a > 0) label.style.color = new StyleColor(op.Color);
                        if (op.FloatA > 0f) label.style.fontSize = op.FloatA;
                    }
                    break;
                case FrameOpKind.DrawRect:
                case FrameOpKind.Box:
                    // Same alpha-0-means-"unset" rule the label path above documents —
                    // it was stated there as already true "for containers below", but the
                    // guard was never actually applied here. `Gui.Box` records no colour
                    // at all, so op.Color is default (0,0,0,0); writing it unconditionally
                    // put a TRANSPARENT INLINE background on every className-styled Box,
                    // and an inline style always beats a stylesheet in UI Toolkit. Net
                    // effect: any `background-color` a USS class set on a Box could never
                    // paint. That is why the HP/SP bar fills (.ro-pro-bar__fill-*) and the
                    // stats portrait inset rendered invisible while their tracks — Rows,
                    // which take a different path — themed correctly.
                    if (op.Color.a > 0) element.style.backgroundColor = new StyleColor(op.Color);
                    ApplySizeIfSet(element, in op);
                    // V2-UI-PARITY.3.1: Box's height-max (Size.Max on Box's height arg)
                    // reuses MaxHeight the same way RowBegin/ColumnBegin/ScrollBegin
                    // already do below — Box never read it before this.
                    if (op.MaxHeight > 0f) element.style.maxHeight = op.MaxHeight;
                    // v0 default chrome: every filled rect gets a small corner
                    // radius so stock components (Button, Toggle pill, ...)
                    // don't read as bare UGUI-default flat rectangles.
                    ApplyCornerRadius(element, Maqui.Components.MaquiTheme.CornerRadius);
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
                    ApplyImage(element, op.Text);
                    break;
                case FrameOpKind.TextInputField:
                    ApplySizeIfSet(element, in op);
                    if (element is TextField tf)
                    {
                        ApplyTextFieldProps(tf, op.Text);
                    }
                    break;
                case FrameOpKind.SliderField:
                    if (element is Slider slider) ApplySliderProps(slider, in op);
                    break;
                case FrameOpKind.DropdownField:
                    if (element is DropdownField dropdown) ApplyDropdownProps(dropdown, op.Text);
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
                        element.style.paddingLeft = Maqui.Components.MaquiTheme.ContainerPadding;
                        element.style.paddingRight = Maqui.Components.MaquiTheme.ContainerPadding;
                        ApplyCornerRadius(element, Maqui.Components.MaquiTheme.PanelCornerRadius);
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
                StyleTextField(tf);
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


        /// <summary>
        /// Paint a stock <c>TextField</c> as a Maqui input.
        ///
        /// <para>WHY THIS EXISTS. Untouched, UI Toolkit's TextField is a white box with a
        /// 12px font, its own margins, and ~1px of usable vertical slack once a caller pins
        /// a fixed height on the root — which is what <see cref="ApplySizeIfSet"/> does for
        /// every Maqui input. The visible result is a tall pale rectangle with small text
        /// clipped along its bottom edge: mostly blank space, and the one glyph row that
        /// matters cut in half.</para>
        ///
        /// <para>The fix has to reach the INNER element. A BaseField's background, border
        /// and padding are painted by its <c>unity-text-input</c> child, not by the root, so
        /// styling <c>tf.style</c> alone changes nothing you can see. Font size and text
        /// color DO inherit, so those are set once on the root.</para>
        ///
        /// <para>Called once per TextField, on first bind. TextFields are never pooled
        /// (see <see cref="Recycle"/> — they drop on the floor), so there is no reuse path
        /// that could strip this chrome back off.</para>
        /// </summary>
        private static void StyleTextField(TextField tf)
        {
            // Inherited by the inner text element; set here so both the input and any
            // future decoration on the root read at the same size.
            tf.style.fontSize = Maqui.Components.MaquiTheme.InputFontSize;
            tf.style.color = new StyleColor((Color)Maqui.Components.MaquiTheme.InputText);
            // A BaseField ships with vertical margin. Against the fixed height the size
            // op writes, that margin comes straight out of the text's own box.
            tf.style.marginTop = 0f;
            tf.style.marginBottom = 0f;
            tf.style.marginLeft = 0f;
            tf.style.marginRight = 0f;

            var input = tf.Q("unity-text-input") ?? tf;
            input.style.flexGrow = 1f;
            input.style.marginTop = 0f;
            input.style.marginBottom = 0f;
            input.style.marginLeft = 0f;
            input.style.marginRight = 0f;
            input.style.paddingLeft = Maqui.Components.MaquiTheme.InputPaddingHorizontal;
            input.style.paddingRight = Maqui.Components.MaquiTheme.InputPaddingHorizontal;
            input.style.paddingTop = Maqui.Components.MaquiTheme.InputPaddingVertical;
            input.style.paddingBottom = Maqui.Components.MaquiTheme.InputPaddingVertical;
            input.style.backgroundColor = new StyleColor(Maqui.Components.MaquiTheme.InputBackground);
            input.style.color = new StyleColor((Color)Maqui.Components.MaquiTheme.InputText);
            input.style.fontSize = Maqui.Components.MaquiTheme.InputFontSize;
            // Middle-left rather than the default upper-left: with the row height fixed,
            // top-aligned text sits against the border and clips.
            input.style.unityTextAlign = TextAnchor.MiddleLeft;
            input.style.borderTopWidth = 1f;
            input.style.borderBottomWidth = 1f;
            input.style.borderLeftWidth = 1f;
            input.style.borderRightWidth = 1f;
            SetBorderColor(input, Maqui.Components.MaquiTheme.InputBorder);
            ApplyCornerRadius(input, Maqui.Components.MaquiTheme.CornerRadius);

            // Focus is otherwise invisible once the stock blue outline is overpainted.
            tf.RegisterCallback<FocusInEvent>(_ =>
                SetBorderColor(input, Maqui.Components.MaquiTheme.InputBorderFocus));
            tf.RegisterCallback<FocusOutEvent>(_ =>
                SetBorderColor(input, Maqui.Components.MaquiTheme.InputBorder));
        }

        private static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        private readonly HashSet<TextField> _textFieldKeys = new();
        private readonly HashSet<VisualElement> _boundValueControls = new();

        /// <summary>WL.0: bind a native <c>Slider</c> to <see cref="Gui.FloatInputs"/>.
        /// Payload is the store key; FloatA/B/C are value/min/max.
        ///
        /// <para>Mirrors <see cref="ApplyTextFieldProps"/> exactly: subscribe once,
        /// then re-sync from the store WITHOUT notifying. Re-syncing with a notify
        /// would echo the store's own value back through the callback every frame and
        /// fight the user mid-drag.</para></summary>
        private void ApplySliderProps(Slider slider, in FrameOp op)
        {
            // A native Slider has no intrinsic width. Dropped into a flex Row with no
            // flex-grow — every current caller, since Slider() exposes no size parameter —
            // it collapses to 0px and renders as an unusable sliver (ORO's config window
            // measured 0x24). A floor, not a fixed width: a larger explicit width wins.
            //
            // Applied HERE and not in the factory: ResetPooledStyles clears minWidth when
            // an element is recycled, so a factory-time floor survives only until the
            // slider's first reuse. That regression is exactly what this comment is for.
            slider.style.minWidth = 120f;

            string storeKey = op.Text;
            float value = op.FloatA, min = op.FloatB, max = op.FloatC;

            if (max > min)
            {
                slider.lowValue = min;
                slider.highValue = max;
            }

            if (!_boundValueControls.Contains(slider))
            {
                _boundValueControls.Add(slider);
                if (_gui != null && storeKey != null && !_gui.FloatInputs.Has(storeKey))
                    _gui.FloatInputs.Set(storeKey, value);

                slider.SetValueWithoutNotify(_gui != null && storeKey != null
                    ? _gui.FloatInputs.Get(storeKey, value)
                    : value);

                if (_gui != null && storeKey != null)
                {
                    var localKey = storeKey;
                    slider.RegisterValueChangedCallback(evt => _gui.FloatInputs.Set(localKey, evt.newValue));
                }
            }
            else if (_gui != null && storeKey != null)
            {
                float cur = _gui.FloatInputs.Get(storeKey, value);
                if (!Mathf.Approximately(slider.value, cur)) slider.SetValueWithoutNotify(cur);
            }
        }

        /// <summary>WL.0: bind a native <c>DropdownField</c> to
        /// <see cref="Gui.TextInputs"/>. Payload is <c>key|selected|opt1|opt2…</c>.</summary>
        private void ApplyDropdownProps(DropdownField dropdown, string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;

            var parts = payload.Split('|');
            string storeKey = parts[0];
            string selected = parts.Length > 1 ? parts[1] : string.Empty;
            var options = parts.Skip(2).Where(p => !string.IsNullOrEmpty(p)).ToList();

            // Rebuild choices only when they actually differ — reassigning the list
            // every frame would reset the popup's own selection state.
            if (options.Count > 0 && (dropdown.choices == null || !dropdown.choices.SequenceEqual(options)))
                dropdown.choices = options;

            if (!_boundValueControls.Contains(dropdown))
            {
                _boundValueControls.Add(dropdown);
                if (_gui != null && storeKey != null && !_gui.TextInputs.Has(storeKey))
                    _gui.TextInputs.Set(storeKey, selected);

                dropdown.SetValueWithoutNotify(_gui != null && storeKey != null
                    ? _gui.TextInputs.Get(storeKey, selected)
                    : selected);

                if (_gui != null && storeKey != null)
                {
                    var localKey = storeKey;
                    dropdown.RegisterValueChangedCallback(evt => _gui.TextInputs.Set(localKey, evt.newValue));
                }
            }
            else if (_gui != null && storeKey != null)
            {
                var cur = _gui.TextInputs.Get(storeKey, selected);
                if (dropdown.value != cur) dropdown.SetValueWithoutNotify(cur);
            }
        }

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

        // Maqui.AlignItems -> UnityEngine.UIElements.Align. The Unity enum is
        // fully qualified: this file is in namespace Maqui AND has a
        // `using UnityEngine.UIElements`, and Maqui declares its own `Align`
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
            // Fit/Ratio still borrow UI Toolkit's own flex layout; Pixels, Expand
            // and Percentage are honored explicitly. Percentage was added for
            // ProgressBar (VPU.1) — a fill that is `value` fraction of its track —
            // and is broadly useful for any bar/meter that should track its parent.
            SizeKind wKind = (SizeKind)(int)op.FloatA;
            SizeKind hKind = (SizeKind)(int)op.FloatC;
            if (wKind == SizeKind.Pixels && op.FloatB > 0f) element.style.width = op.FloatB;
            if (hKind == SizeKind.Pixels && op.FloatD > 0f) element.style.height = op.FloatD;
            // Percentage: fraction (0..1) of the parent's corresponding axis. Clamp
            // the low end at 0 so an empty bar collapses cleanly; allow >1 through
            // since the Size doc says overflow is permitted.
            if (wKind == SizeKind.Percentage)
                element.style.width = new StyleLength(new Length(Mathf.Max(0f, op.FloatB) * 100f, LengthUnit.Percent));
            if (hKind == SizeKind.Percentage)
                element.style.height = new StyleLength(new Length(Mathf.Max(0f, op.FloatD) * 100f, LengthUnit.Percent));
            // Expand: grow to fill remaining space along the parent's main axis —
            // e.g. a Spacer(Size.Expand()) between a title and a close button
            // pushes the button to the far edge of a Row. Same underlying Yoga
            // property regardless of which axis (width/height) carried the Expand
            // kind, so either slot maps to flexGrow.
            if (wKind == SizeKind.Expand) element.style.flexGrow = op.FloatB > 0f ? op.FloatB : 1f;
            if (hKind == SizeKind.Expand) element.style.flexGrow = op.FloatD > 0f ? op.FloatD : 1f;

            // V2-UI-PARITY.3.1 — min/max constraints, unconditional (outside the SizeKind
            // switch above) so they apply even to Fit/Ratio nodes that get no explicit
            // width/height style today. This is the actual "stop shrinking below
            // min-content" primitive: a Fit column with WithMin(200) still hugs its
            // content up to 200px, then refuses to shrink further under pressure.
            if (op.WidthMin > 0f) element.style.minWidth = op.WidthMin;
            if (op.WidthMax > 0f) element.style.maxWidth = op.WidthMax;
            if (op.HeightMin > 0f) element.style.minHeight = op.HeightMin;
        }

        /// <summary>Resolve a DrawImage key and paint it as the element's
        /// background, preferring the atlas sprite loader over the loose-texture
        /// loader. Aspect is preserved (Contain) so a non-square source
        /// letterboxes inside the requested box instead of stretching. A null
        /// resolution clears any pooled leftover background so a recycled
        /// element never shows a previous icon.</summary>
        private void ApplyImage(VisualElement element, string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (_spriteLoader != null)
                {
                    var sprite = _spriteLoader.Resolve(key);
                    if (sprite != null)
                    {
                        element.style.backgroundImage = new StyleBackground(sprite);
                        element.style.backgroundSize =
                            new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                        return;
                    }
                }
                if (_imageLoader != null)
                {
                    var tex = _imageLoader.Resolve(key);
                    if (tex != null)
                    {
                        element.style.backgroundImage = new StyleBackground(tex);
                        element.style.backgroundSize =
                            new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                        return;
                    }
                }
            }
            // No loader, empty key, or unresolved: leave no stale background on a
            // recycled element.
            element.style.backgroundImage = StyleKeyword.Null;
        }

        /// <summary>
        /// Clear every inline style the backend is capable of writing, before a recycled
        /// element is handed to its next occupant.
        ///
        /// <para><b>Why this is mandatory.</b> <c>ApplyProps</c>/<c>ApplySizeIfSet</c> only
        /// ever SET properties — they set width when the op says Pixels, flexGrow when it
        /// says Expand, and so on — and never clear the ones the new op is silent about.
        /// The pool is per-kind, so an element always returns as the same op kind, but the
        /// same kind is used for very different ROLES: a <c>Spacer(Size.Expand())</c>
        /// (flexGrow 1) and a <c>Spacer(Size.Pixels(6))</c> are both Spacers. Recycle the
        /// first as the second and it is 6px wide AND still grows.
        ///
        /// <para>That is what wrecked the Settings window specifically: its rows alternate
        /// expand-spacers, fixed-spacers, sliders and dropdowns more than any other V2
        /// window, so it recycles across roles the most. The visible symptoms were a
        /// toggle pill stretched across the row and a knob detached from its pill —
        /// the knob's offset Spacer had inherited flexGrow from an expand-spacer.</para>
        ///
        /// <para>Cleared to <see cref="StyleKeyword.Null"/>, not to a default value: Null
        /// REMOVES the inline style so the element falls back to its USS class, which is
        /// the whole point of the class-based skin. Writing a concrete default here would
        /// reintroduce the inline-beats-stylesheet bug in a different place.</para>
        ///
        /// <para>Done at rent time rather than in ApplyProps so it costs one pass per
        /// recycle instead of one per element per frame.</para>
        /// </summary>
        private static void ResetPooledStyles(VisualElement element)
        {
            // Layout — the group that actually corrupted Settings.
            element.style.width = StyleKeyword.Null;
            element.style.height = StyleKeyword.Null;
            element.style.minWidth = StyleKeyword.Null;
            element.style.maxHeight = StyleKeyword.Null;
            element.style.flexGrow = StyleKeyword.Null;
            element.style.alignItems = StyleKeyword.Null;
            element.style.paddingLeft = StyleKeyword.Null;
            element.style.paddingRight = StyleKeyword.Null;
            // V2-UI-PARITY.3.1: maxWidth/minHeight are the new min/max constraint half
            // ApplySizeIfSet now writes (see above) — must be reset like minWidth/maxHeight
            // already were, or a recycled element keeps a stale constraint from its
            // previous occupant's role.
            element.style.maxWidth = StyleKeyword.Null;
            element.style.minHeight = StyleKeyword.Null;
            // V2-UI-PARITY.3.5: pre-existing gap found while auditing the above — these
            // are set once at element-creation time (Build(), below) for Row/Column/
            // ClipBox but were never in the reset set. The pool funnels every
            // non-specialized element (Row/Column/ClipBox/Box/Spacer/DrawRect/DrawLine/
            // DrawCircle/DrawImage) through ONE FrameOpKind.Box-keyed bucket on Recycle
            // (see TryPop/Push below), so a Box rented from that bucket could silently
            // inherit a former Row's flexDirection:Row or a former ClipBox's
            // overflow:Hidden. Currently harmless (every live Box call site is
            // childless), but cheap to close now that 3.1 already touches this method.
            element.style.flexDirection = StyleKeyword.Null;
            element.style.overflow = StyleKeyword.Null;

            // Paint.
            element.style.backgroundColor = StyleKeyword.Null;
            element.style.backgroundImage = StyleKeyword.Null;
            element.style.color = StyleKeyword.Null;
            element.style.fontSize = StyleKeyword.Null;
            element.style.borderTopLeftRadius = StyleKeyword.Null;
            element.style.borderTopRightRadius = StyleKeyword.Null;
            element.style.borderBottomLeftRadius = StyleKeyword.Null;
            element.style.borderBottomRightRadius = StyleKeyword.Null;
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
