Shader "Little Rush/VFX/Chest Rainbow Line"
{
    Properties
    {
        [MainTexture] _BaseMap("Particle Texture", 2D) = "white" {}
        _Glow("Glow", Range(0, 5)) = 3
        _VisibleSegments("Visible Segments", Float) = 14
        _SegmentsPerSecond("Segments Per Second", Float) = 5
        _AtlasRows("Atlas Rows", Float) = 8
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0, 1)) = 0.01
        _RainbowPink("Rainbow 1 - Pink", Color) = (1, 0.1882353, 0.2823529, 1)
        _RainbowOrange("Rainbow 2 - Orange", Color) = (1, 0.5411765, 0.1411765, 1)
        _RainbowYellow("Rainbow 3 - Yellow", Color) = (1, 0.8862745, 0.2196078, 1)
        _RainbowGreen("Rainbow 4 - Green", Color) = (0.1921569, 0.8392157, 0.427451, 1)
        _RainbowCyan("Rainbow 5 - Cyan", Color) = (0.145098, 0.7843137, 0.9568627, 1)
        _RainbowBlue("Rainbow 6 - Blue", Color) = (0.2392157, 0.3882353, 1, 1)
        _RainbowPurple("Rainbow 7 - Purple", Color) = (0.4705882, 0.2392157, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Glow;
                float _VisibleSegments;
                float _SegmentsPerSecond;
                float _AtlasRows;
                float _AlphaClipThreshold;
                half4 _RainbowPink;
                half4 _RainbowOrange;
                half4 _RainbowYellow;
                half4 _RainbowGreen;
                half4 _RainbowCyan;
                half4 _RainbowBlue;
                half4 _RainbowPurple;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half3 GetRainbowColor(float index)
            {
                if (index < 0.5)
                    return _RainbowPink.rgb;
                if (index < 1.5)
                    return _RainbowOrange.rgb;
                if (index < 2.5)
                    return _RainbowYellow.rgb;
                if (index < 3.5)
                    return _RainbowGreen.rgb;
                if (index < 4.5)
                    return _RainbowCyan.rgb;
                if (index < 5.5)
                    return _RainbowBlue.rgb;

                return _RainbowPurple.rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Texture Sheet Animation supplies atlas UVs. Recover the 0..1
                // coordinate inside the selected row so the bands fill the ray.
                float linePosition = frac(input.uv.y * _AtlasRows);
                float atlasRowStart = input.uv.y - linePosition / _AtlasRows;
                float atlasRowHeight = rcp(_AtlasRows);

                // The source ray frames have an uneven alpha pattern from bottom
                // to top. Build one uniform vertical mask from several slices so
                // every rainbow segment has the same visible length.
                half maskAlpha = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap,
                    float2(input.uv.x, atlasRowStart + atlasRowHeight * 0.1)).a;
                maskAlpha = max(maskAlpha, SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap,
                    float2(input.uv.x, atlasRowStart + atlasRowHeight * 0.3)).a);
                maskAlpha = max(maskAlpha, SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap,
                    float2(input.uv.x, atlasRowStart + atlasRowHeight * 0.5)).a);
                maskAlpha = max(maskAlpha, SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap,
                    float2(input.uv.x, atlasRowStart + atlasRowHeight * 0.7)).a);
                maskAlpha = max(maskAlpha, SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap,
                    float2(input.uv.x, atlasRowStart + atlasRowHeight * 0.9)).a);

                half alpha = maskAlpha * input.color.a;
                clip(alpha - _AlphaClipThreshold);

                float segment = floor(
                    linePosition * _VisibleSegments - _Time.y * _SegmentsPerSecond);
                float colorIndex = segment - floor(segment / 7.0) * 7.0;
                half3 color = GetRainbowColor(colorIndex) * _Glow;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
