// Glass over the LCD: an inner shadow where the bezel meets the display and one soft glare
// blob, both computed from the quad's UVs so there is no gradient texture to band under ASTC.
// Sits on the ScreenFX layer below the HUD (GDD 5.1). Unlit, one transparent pass.
Shader "NightCafe/LcdGlass"
{
    Properties
    {
        _VignetteColor ("Vignette colour", Color) = (0.071, 0.051, 0.035, 1)
        _VignetteStrength ("Vignette strength", Range(0, 1)) = 0.55
        _VignettePower ("Vignette falloff", Range(0.5, 8)) = 3
        _GlareColor ("Glare colour", Color) = (1, 0.92, 0.75, 1)
        _GlareStrength ("Glare strength", Range(0, 0.3)) = 0.05
        _GlareCenter ("Glare centre (uv)", Vector) = (0.22, 0.86, 0, 0)
        _GlareSize ("Glare size", Range(0.1, 1.5)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LcdGlass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _VignetteColor;
                half _VignetteStrength;
                half _VignettePower;
                half4 _GlareColor;
                half _GlareStrength;
                float4 _GlareCenter;
                half _GlareSize;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;

                // Mostly square so the shadow hugs the bezel, a little round so corners deepen.
                float square = max(abs(p.x), abs(p.y));
                float round = length(p) * 0.7071;
                float edge = lerp(square, round, 0.35);
                half vignette = pow(saturate(edge), _VignettePower) * _VignetteStrength;

                float2 g = (input.uv - _GlareCenter.xy) / _GlareSize;
                half glare = exp(-dot(g, g) * 2.0) * _GlareStrength;

                half alpha = saturate(vignette + glare);
                half3 colour = lerp(_GlareColor.rgb, _VignetteColor.rgb, vignette / max(alpha, 1e-4));
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
