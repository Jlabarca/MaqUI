// SPDX-License-Identifier: MIT
// MaqUI v2 — SDF-aware backend interface sketch.
//
// =====================================================================
// MAQUIV2.3 — BLIND DRAFT (interface only). Operator wires real impl
// after MAQUIV2.3.3 ShaderGraph + Material assets exist.
// =====================================================================
//
// This file is a *sketch* of how DrawRect/DrawCircle frame ops route into
// an SDF-rendered VisualElement when the operator has authored:
//
//   - Runtime/V2/Shaders/MaquiSDF.hlsl     (shipped this run)
//   - Runtime/V2/Shaders/MaquiSDF.shadergraph  (Editor-required, pending)
//   - Runtime/V2/Materials/MaquiSDF.mat        (Editor-required, pending)
//
// Until those assets exist, UIToolkitBackend (Runtime/V2/UIToolkitBackend.cs)
// keeps handling DrawRect/DrawCircle via flat style.backgroundColor +
// borderRadius — visually adequate for P2 hello-world, missing only the
// inner/outer shadow chrome that's P3's actual exit gate.
//
// Implementation strategy when 3.3 lands (operator):
//   1. Construct an SdfBackend wrapping a UIToolkitBackend.
//   2. Override CreateElement(DrawRect | DrawCircle) to:
//        a. Create a VisualElement
//        b. Assign customStyle / a per-instance Material via reflection on
//           VisualElement.style.unityBackgroundImageTintColor + a
//           PanelRenderer custom material binding (URP 17 path).
//        c. Set MaterialPropertyBlock float4 _ShapeParams (size, radius,
//           shadow offset, shadow softness) per FrameOp.
//   3. Delegate everything else (RowBegin/ColumnBegin/DrawText/...) to the
//      wrapped backend.
//
// The decorator pattern keeps the SDF path opt-in. Default backend stays
// flat-styled. ORO/Rompe enables SDF only on chrome-heavy windows that
// need shadows (popups, tooltips, callouts).

using UnityEngine;

namespace Maqui.V2
{
    /// <summary>
    /// Per-shape parameters packed into a <see cref="UnityEngine.Vector4"/>
    /// for upload to <c>MaquiSDF.shadergraph</c> via
    /// <see cref="MaterialPropertyBlock"/>.
    ///
    /// <para>Layout reused across rect/rounded-rect/circle by convention:</para>
    /// <list type="bullet">
    ///   <item><c>HalfSizeX</c> — half-width in local pixels.</item>
    ///   <item><c>HalfSizeY</c> — half-height in local pixels.</item>
    ///   <item><c>CornerRadius</c> — corner radius in local pixels (0 = sharp).</item>
    ///   <item><c>StrokeHalfWidth</c> — stroke half-width (0 = fill-only).</item>
    /// </list>
    /// </summary>
    public readonly struct SdfShapeParams
    {
        public float HalfSizeX { get; }
        public float HalfSizeY { get; }
        public float CornerRadius { get; }
        public float StrokeHalfWidth { get; }

        public SdfShapeParams(float halfSizeX, float halfSizeY, float cornerRadius, float strokeHalfWidth)
        {
            HalfSizeX = halfSizeX;
            HalfSizeY = halfSizeY;
            CornerRadius = cornerRadius;
            StrokeHalfWidth = strokeHalfWidth;
        }

        public Vector4 ToVector4() => new Vector4(HalfSizeX, HalfSizeY, CornerRadius, StrokeHalfWidth);
    }

    /// <summary>
    /// Per-shape shadow parameters (RGBA color + offset + softness).
    /// Uploaded as two Vector4s to keep shader uniforms tight.
    /// </summary>
    public readonly struct SdfShadowParams
    {
        public Color32 Color { get; }
        public float OffsetX { get; }
        public float OffsetY { get; }
        public float Softness { get; }
        public bool Inner { get; }

        public SdfShadowParams(Color32 color, float offsetX, float offsetY, float softness, bool inner)
        {
            Color = color;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Softness = softness;
            Inner = inner;
        }
    }

    /// <summary>
    /// Decorator <see cref="IBackend"/> that routes shape ops through the SDF
    /// material when authored, delegating all other ops to a wrapped backend.
    ///
    /// <para><b>Status: blind draft (interface only).</b> Implementation
    /// commented out until MAQUIV2.3.3 ShaderGraph asset lands. Currently a
    /// pass-through to the wrapped backend so it compiles + tests stay green.</para>
    /// </summary>
    public sealed class SdfBackend : IBackend
    {
        private readonly IBackend _inner;

        public SdfBackend(IBackend inner)
        {
            _inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
        }

        public int Root => _inner.Root;

        public void BeginReconcile() => _inner.BeginReconcile();
        public void EndReconcile() => _inner.EndReconcile();
        public int CreateElement(in FrameOp op) => _inner.CreateElement(in op);
        public void UpdateElement(int handle, in FrameOp op) => _inner.UpdateElement(handle, in op);
        public void SetParent(int child, int parent, int siblingIndex) => _inner.SetParent(child, parent, siblingIndex);
        public void Recycle(int handle) => _inner.Recycle(handle);

        // ---------- Pending real impl (MAQUIV2.3.3+) ----------
        //
        // When the ShaderGraph asset lands:
        //
        //   1. Inject a Material reference in the ctor:
        //        public SdfBackend(IBackend inner, Material sdfMaterial) { ... }
        //   2. Override CreateElement(DrawRect|DrawCircle) to:
        //        var el = ... ; // VisualElement with custom material binding
        //        ApplySdfParams(el, in op);
        //   3. Override UpdateElement(DrawRect|DrawCircle) likewise.
        //   4. Helper:
        //        private void ApplySdfParams(VisualElement el, in FrameOp op) {
        //            var mpb = new MaterialPropertyBlock();
        //            mpb.SetVector("_ShapeParams", new Vector4(
        //                op.FloatB * 0.5f, op.FloatD * 0.5f, /* corner */ 0f, /* stroke */ 0f));
        //            // ... + shadow params from a future FrameOp extension
        //        }
        //
        // The FrameOp struct already carries enough payload (FloatA..D, Color)
        // for fill + size; shadow params will need either (a) a new FrameOp
        // variant (DrawRectShadowed) or (b) an out-of-band per-node shadow
        // table keyed by NodeId. Decide at impl time.
    }
}
