// SPDX-License-Identifier: MIT
// MaqUI v2 — FrameOp record. Internal; consumed by the P2 reconciler.

using UnityEngine;

namespace Maqui.V2
{
    /// <summary>
    /// Discriminates a <see cref="FrameOp"/>. Begin/End ops bracket containers so
    /// the linear FrameBuffer can be parsed back into a tree by the reconciler (P2).
    ///
    /// <para><b>Public</b> as of P2 — backends consume <see cref="FrameOp"/>s and
    /// need to read their kind. <see cref="IBackend"/> is a public surface for
    /// out-of-assembly backend authors.</para>
    /// </summary>
    public enum FrameOpKind : byte
    {
        // Layout containers (paired Begin/End):
        RowBegin = 1,
        RowEnd = 2,
        ColumnBegin = 3,
        ColumnEnd = 4,

        // Layout leaves:
        Box = 10,
        Spacer = 11,
        // ClipBoxBegin/End: container like Row/Column but with overflow:Hidden on the backend
        // wrapper. Used by ScrollView for clip-rendering.
        ClipBoxBegin = 12,
        ClipBoxEnd = 13,
        // ScrollBegin/End: container like Row/Column but the backend mints a real
        // UI Toolkit ScrollView (native wheel + drag-scrollbar handling, no
        // contentHeight measurement needed from the caller). See Gui.ScrollBox.
        ScrollBegin = 14,
        ScrollEnd = 15,

        // Draw primitives:
        DrawRect = 20,
        DrawText = 21,
        DrawLine = 22,
        DrawCircle = 23,
        // P8.4: image draw. Backend resolves Text payload as the texture key via IImageLoader.
        DrawImage = 24,

        // Interaction (recorded against a parent Node):
        OnClick = 30,
        OnHover = 31,
        OnHold = 32,
        OnDrag = 33,

        // Data scope:
        ScopeEnter = 40,
        ScopeExit = 41,

        // P8.6: editable text field. Backend creates a UI Toolkit TextField; Text payload
        // is the initial value; backend routes value changes to Gui.TextInputs[ScopePath].
        TextInputField = 50,

        // WL.0: native value-emitting controls. The backend mints the host
        // framework's own Slider/Dropdown and routes value changes into
        // Gui.FloatInputs / Gui.TextInputs — the same swap pattern TextInputField
        // already uses. Replaces hand-drawn widgets that had to re-derive
        // interaction from pointer geometry (and got it wrong).
        SliderField = 51,
        DropdownField = 52,
    }

    /// <summary>
    /// Cross-axis alignment of a container's CHILDREN (Yoga <c>alignItems</c>).
    /// <see cref="Stretch"/> is 0 so it stays the default for every op that
    /// doesn't set one — matching flex's own default and keeping the payload
    /// backward-compatible.
    ///
    /// <para>Distinct from <see cref="Align"/>, which per the spec is a float
    /// 0..1 describing how a node aligns ITSELF within its parent. This is an
    /// enum because Yoga's alignItems is genuinely categorical — "stretch" is
    /// not a point on the 0..1 line.</para>
    /// </summary>
    public enum AlignItems : byte
    {
        Stretch = 0,
        Start = 1,
        Center = 2,
        End = 3,
    }

    /// <summary>
    /// One immediate-mode call captured during a frame. Layout, draw, interaction,
    /// and scope ops all share this struct via a tagged payload — keeps the frame
    /// buffer a single contiguous List with no boxing.
    ///
    /// <para><b>Public</b> as of P2 — <see cref="IBackend"/> consumes FrameOps and
    /// the interface is public so external backends (e.g., future PanGui backend)
    /// can implement it. The fields read like a discriminated union; meaning
    /// depends on <see cref="Kind"/>.</para>
    /// </summary>
    public readonly struct FrameOp
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

        /// <summary>Cross-axis alignment of this container's children. Only read
        /// for container Begin ops (Row/Column/ClipBox/Scroll); ignored by leaves.
        /// FloatA-D are fully spoken for by the width/height (kind, value) pairs,
        /// hence a dedicated field rather than another float slot.</summary>
        public AlignItems AlignItems { get; }

        /// <summary>Max height in pixels for a container; 0 = unset. Distinct from a
        /// fixed height: the container hugs its content and only caps (and, for a
        /// ScrollBox, starts scrolling) once content exceeds this. A fixed height
        /// would instead reserve the full box even when the content is tiny.</summary>
        public float MaxHeight { get; }

        /// <summary>Optional style class for the element this op mints; null = none.
        /// The backend forwards it to the host framework's styling system (USS
        /// <c>AddToClassList</c> for UI Toolkit) so appearance can live in a
        /// stylesheet instead of in draw-op arguments. Deliberately a plain string,
        /// not a Maqui-owned style type: the point is to DELEGATE theming to the
        /// backend, not to grow a second one here (WINDOW-LOOP WL.2.2).</summary>
        public string ClassName { get; }

        public FrameOp(
            FrameOpKind kind,
            int nodeId,
            string scopePath,
            float a = 0f, float b = 0f, float c = 0f, float d = 0f,
            Color32 color = default,
            string text = null,
            AlignItems alignItems = AlignItems.Stretch,
            float maxHeight = 0f,
            string className = null)
        {
            MaxHeight = maxHeight;
            ClassName = className;
            Kind = kind;
            NodeId = nodeId;
            ScopePath = scopePath;
            FloatA = a;
            FloatB = b;
            FloatC = c;
            FloatD = d;
            Color = color;
            Text = text;
            AlignItems = alignItems;
        }

        public override string ToString()
        {
            string path = ScopePath ?? "/";
            return $"[{Kind} #{NodeId} @{path}]";
        }
    }
}
