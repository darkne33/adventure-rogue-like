Shader "Custom/LavaWithContactURP"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0.2, 0.0, 0.0, 1)
        _CenterColor ("Center Color", Color) = (1.0, 0.6, 0.1, 1)
        _ContactColor ("Contact Color", Color) = (1.0, 1.0, 0.5, 1) // цвет при контакте
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 2.0
        _ContactIntensity ("Contact Intensity", Range(0, 3)) = 1.5
        _ContactDistance ("Contact Distance", Range(0, 1)) = 0.1 // метры
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _Radius ("Object Radius", Float) = 5.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _EdgeColor;
                float4 _CenterColor;
                float4 _ContactColor;
                float _EmissionStrength;
                float _ContactIntensity;
                float _ContactDistance;
                float _PulseSpeed;
                float _Radius;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Градиент от центра
                float3 objCenter = unity_ObjectToWorld._m03_m13_m23;
                float distFromCenter = distance(input.positionWS, objCenter);
                float t = saturate(distFromCenter / _Radius);
                half3 baseColor = lerp(_CenterColor.rgb, _EdgeColor.rgb, t);

                // 2. Пульсация
                float pulse = sin(_Time.y * _PulseSpeed) * 0.1 + 1.0;
                baseColor *= pulse;

                // 3. Эффект контакта с другими объектами
                float sceneDepth = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                float fragDepth = input.screenPos.z / input.screenPos.w;
                float depthDiff = (sceneDepth - fragDepth) * _ProjectionParams.z; // в метрах

                // Если другой объект очень близко (или внутри нас)
                float contact = 0;
                if (depthDiff > 0 && depthDiff < _ContactDistance)
                {
                    contact = 1.0 - saturate(depthDiff / _ContactDistance);
                }

                // Добавляем контактный цвет и эмиссию
                half3 contactGlow = _ContactColor.rgb * contact * _ContactIntensity;
                half3 emission = baseColor * _EmissionStrength * (1.0 - t * 0.7) + contactGlow;

                return half4(baseColor + emission, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
