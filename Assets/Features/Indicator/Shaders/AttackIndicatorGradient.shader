Shader "LittleRush/Attack Indicator Gradient"
{
    Properties
    {
        [HDR] _StartColor ("Start Color", Color) = (1.6, 0.01, 0.005, 0.85)
        [HDR] _EndColor ("End Color", Color) = (0.55, 0.005, 0.002, 0.08)
        [Enum(Radial, 0, Directional, 1, UV, 2)] _GradientMode ("Gradient Mode", Float) = 0
        _GradientOrigin ("Gradient Origin (Object Space)", Vector) = (0, 0, 0, 0)
        _GradientAxis ("Gradient Axis (Object Space)", Vector) = (0, 0, 1, 0)
        _GradientLength ("Gradient Length", Float) = 0.5
        _GradientPower ("Gradient Power", Range(0.2, 4)) = 1
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _StartColor;
                half4 _EndColor;
                float4 _GradientOrigin;
                float4 _GradientAxis;
                float _GradientMode;
                float _GradientLength;
                float _GradientPower;
                float _UseVertexColor;
                float _Opacity;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 offset = input.positionOS - _GradientOrigin.xyz;
                float gradientLength = max(_GradientLength, 0.0001);
                float t;

                if (_GradientMode < 0.5)
                {
                    t = length(offset.xz) / gradientLength;
                }
                else if (_GradientMode < 1.5)
                {
                    float3 axis = _GradientAxis.xyz / max(length(_GradientAxis.xyz), 0.0001);
                    t = dot(offset, axis) / gradientLength;
                }
                else
                {
                    t = input.uv.x;
                }

                t = smoothstep(0.0, 1.0, pow(saturate(t), max(_GradientPower, 0.0001)));
                half4 color = lerp(_StartColor, _EndColor, t);

                if (_UseVertexColor > 0.5)
                    color *= input.color;

                color.a *= saturate(_Opacity);
                return color;
            }
            ENDHLSL
        }
    }
}
