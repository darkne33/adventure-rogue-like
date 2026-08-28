Shader "UI/LittleRush/CharacterPreviewColorCorrection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Saturation ("Saturation", Range(-100, 100)) = 0
        _Contrast ("Contrast", Range(-100, 100)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Saturation;
            float _Contrast;

            float3 LinearToPreviewLogC(float3 color)
            {
                return 0.244161 * log10(max(5.555556 * color + 0.047996, 0.000001))
                    + 0.386036;
            }

            float3 PreviewLogCToLinear(float3 color)
            {
                return (pow(10.0, (color - 0.386036) / 0.244161) - 0.047996)
                    / 5.555556;
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float3 correctedColor = max(0.0, color.rgb);
                float contrastMultiplier = 1.0 + _Contrast * 0.01;
                float saturationMultiplier = 1.0 + _Saturation * 0.01;

                float3 logColor = LinearToPreviewLogC(correctedColor);
                logColor = (logColor - 0.4135884) * contrastMultiplier + 0.4135884;
                correctedColor = max(0.0, PreviewLogCToLinear(logColor));

                float luminance = dot(correctedColor,
                    float3(0.2126729, 0.7151522, 0.0721750));
                correctedColor = luminance.xxx
                    + saturationMultiplier * (correctedColor - luminance.xxx);
                correctedColor = saturate(correctedColor);

                color.rgb = correctedColor;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
