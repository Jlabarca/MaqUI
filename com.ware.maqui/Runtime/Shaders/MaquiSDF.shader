// SPDX-License-Identifier: MIT
// MaqUI v2 — MaquiSDF.shader wrapper around MaquiSDF.hlsl.
//
// URP-compatible Unlit shader that renders a rounded rect with optional
// outer shadow using the SDF math from MaquiSDF.hlsl. Per-instance
// properties (_HalfSize, _CornerRadius, _Color, _ShadowColor, _ShadowSoft)
// can be driven by MaterialPropertyBlock. This is the minimum wrapper
// proving MaquiSDF.hlsl is wireable; a fuller ShaderGraph asset can
// expose the same math more flexibly later.

Shader "Maqui/V2/MaquiSDF"
{
    Properties
    {
        _Color ("Fill Color", Color) = (0.31, 0.55, 0.86, 1)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _HalfSize ("Half Size (px)", Vector) = (100, 60, 0, 0)
        _CornerRadius ("Corner Radius (px)", Float) = 12
        _ShadowSoft ("Shadow Softness (px)", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "MaquiSDF.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPx : TEXCOORD1;
            };

            float4 _Color;
            float4 _ShadowColor;
            float4 _HalfSize;
            float _CornerRadius;
            float _ShadowSoft;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                // Map UV [0,1] to pixel-space centered on the shape.
                OUT.localPx = (IN.uv - 0.5) * (_HalfSize.xy * 2.0);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float d;
                Sdf_RoundedRect_float(IN.localPx, _HalfSize.xy, _CornerRadius, d);

                float4 fill;
                Sdf_FillAA_float(d, _Color, fill);

                float4 shadow;
                Sdf_OuterShadow_float(d, _ShadowSoft, _ShadowColor, shadow);

                float4 over;
                Sdf_Over_float(fill, shadow, over);
                return half4(over);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
