// SPDX-License-Identifier: MIT
// MaqUI v2 — Signed Distance Field primitives + compositors.
//
// =====================================================================
// MAQUIV2.3 — BLIND DRAFT. Operator must wrap in ShaderGraph to validate.
// =====================================================================
//
// This file targets URP ShaderGraph "Custom Function" nodes. Each function
// follows ShaderGraph's "void with out param" convention and uses the
// `_float` suffix for the float-precision variant. Wire in ShaderGraph as:
//
//   Custom Function node:
//     Source: File
//     File:   Packages/com.ware.maqui/Runtime/V2/Shaders/MaquiSDF.hlsl
//     Name:   Sdf_RoundedRect_float     (etc.)
//     Inputs/Outputs match the function signature below.
//
// Verification path (operator, in Unity):
//   1. Create ShaderGraph asset → add Custom Function node → set Source: File
//   2. Set File → MaquiSDF.hlsl ; Name → e.g. Sdf_RoundedRect_float
//   3. ShaderGraph reports compile errors inline if function not found or
//      types mismatch. Author once per shape/compositor; chain via Lerp.
//   4. Master Stack: Unlit, transparent, Base Color = composited result.
//
// References:
//   - Inigo Quilez SDF cheatsheet — https://iquilezles.org/articles/distfunctions2d/
//   - Unity ShaderGraph Custom Function — Subgraph Authoring docs (URP 17.x)
//
// No #include directives so the file works inside ShaderGraph's HLSL
// sandbox (it provides UnityCG-like primitives implicitly). If the operator
// finds a ShaderGraph build flagging a missing intrinsic, add
//   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// at the top and re-test.

#ifndef MAQUI_SDF_INCLUDED
#define MAQUI_SDF_INCLUDED

// ----- Primitives -----------------------------------------------------
//
// Convention: P is the fragment position in local UI-pixel space, centered
// on the shape. Returns: signed distance — negative inside, zero on edge,
// positive outside.

// Axis-aligned rectangle, centered at origin.
//   HalfSize: half-extents (width/2, height/2) in local pixels.
void Sdf_Rect_float(float2 P, float2 HalfSize, out float Out)
{
    float2 d = abs(P) - HalfSize;
    Out = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// Rounded rectangle. Radius must be <= min(HalfSize.x, HalfSize.y) — caller's
// responsibility. The standard Inigo Quilez formulation: shrink the rect by
// Radius on each side, compute distance to the shrunk rect, then re-expand.
void Sdf_RoundedRect_float(float2 P, float2 HalfSize, float Radius, out float Out)
{
    float2 q = abs(P) - HalfSize + Radius;
    Out = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - Radius;
}

// Per-corner radii — useful for tabs, sliders, pill shapes.
//   Radii: (top-right, bottom-right, top-left, bottom-left) clockwise from TR.
void Sdf_RoundedRectPerCorner_float(float2 P, float2 HalfSize, float4 Radii, out float Out)
{
    // Select the radius for the quadrant P is in.
    float r;
    r = (P.x > 0.0) ? Radii.x : Radii.z;   // top-right or top-left if y>0
    r = (P.y > 0.0) ? r : ((P.x > 0.0) ? Radii.y : Radii.w);

    float2 q = abs(P) - HalfSize + r;
    Out = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void Sdf_Circle_float(float2 P, float Radius, out float Out)
{
    Out = length(P) - Radius;
}

// ----- Compositors ----------------------------------------------------
//
// All compositors take an existing distance value `D` (from one of the
// primitives above) and return a premultiplied-alpha RGBA contribution.
// Caller composites layers via standard over-operator (Lerp by alpha).

// AA fill. Anti-aliased boundary via fwidth(D) — the screen-space derivative
// of the distance field, the standard cheap-and-good technique.
void Sdf_FillAA_float(float D, float4 Color, out float4 Out)
{
    float aa = fwidth(D);
    // Coverage = 1 inside, 0 outside, smoothly transitioning across one
    // pixel of derivative.
    float coverage = 1.0 - smoothstep(-aa, aa, D);
    Out = float4(Color.rgb * coverage * Color.a, coverage * Color.a);
}

// Stroke — a band of width HalfWidth centered on the edge (D=0).
void Sdf_Stroke_float(float D, float HalfWidth, float4 Color, out float4 Out)
{
    float aa = fwidth(D);
    // Distance to the band center-line is |D|; band coverage is the
    // smoothstep complement around HalfWidth.
    float coverage = 1.0 - smoothstep(HalfWidth - aa, HalfWidth + aa, abs(D));
    Out = float4(Color.rgb * coverage * Color.a, coverage * Color.a);
}

// Outer drop shadow. Falls off over `Radius` pixels outside the shape.
//   D:      base shape SDF
//   Radius: shadow softness in pixels (a.k.a. blur)
//   Offset: shadow offset in pixels — caller pre-applies by passing
//           Sdf_*_float(P - Offset, ...) as D.
void Sdf_OuterShadow_float(float D, float Radius, float4 Color, out float4 Out)
{
    // Coverage ramps from 0 (deep outside) to 1 (just outside the edge).
    float coverage = 1.0 - smoothstep(0.0, max(Radius, 0.0001), D);
    // Multiply by (D > -fwidth(D)) to keep shadow outside the fill — caller
    // composites fill *on top* via Lerp(shadow, fill, fillAlpha).
    Out = float4(Color.rgb * coverage * Color.a, coverage * Color.a);
}

// Inner shadow. Falls off inside the shape (D < 0).
void Sdf_InnerShadow_float(float D, float Radius, float4 Color, out float4 Out)
{
    // Inside-band: -Radius <= D <= 0. Coverage ramps from 1 at edge to 0
    // at -Radius inside.
    float coverage = smoothstep(-max(Radius, 0.0001), 0.0, D);
    // Clip to inside.
    coverage *= step(D, 0.0);
    Out = float4(Color.rgb * coverage * Color.a, coverage * Color.a);
}

// ----- Combinators ----------------------------------------------------

// Standard over-operator: A over B (A is on top).
// Inputs are premultiplied-alpha RGBA.
void Sdf_Over_float(float4 A, float4 B, out float4 Out)
{
    Out = A + B * (1.0 - A.a);
}

// SDF union (min) and intersection (max). Useful for composite shapes —
// e.g., rounded-rect with a notch = max(rect, -circle).
void Sdf_Union_float(float A, float B, out float Out)
{
    Out = min(A, B);
}

void Sdf_Intersect_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Sdf_Subtract_float(float A, float B, out float Out)
{
    Out = max(A, -B);
}

#endif // MAQUI_SDF_INCLUDED
