Shader "Retro 3D Shader Pack/Retro_Lit_URP_Final"
{
    Properties
    {
        _MainTex("Albedo Texture", 2D) = "white" {}
        _Color("Color Tint", Color) = (1,1,1,1)

        _VertJitter("Vertex Jitter", Range(0,1)) = 0
        _AffineMapIntensity("Affine Mapping", Range(0,1)) = 1
        _DrawDist("Draw Distance", Float) = 0

        _ReceiveShadows("Receive Shadows", Range(0,1)) = 1

        _HitColor("Hit Color", Color) = (1,0,0,1)
        _HitPower("Hit Power", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_ST;
            float4 _Color;

            float _VertJitter;
            float _AffineMapIntensity;
            float _DrawDist;
            float _ReceiveShadows;

            float4 _HitColor;
            float _HitPower;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float3 uv_affine : TEXCOORD5;
            };

            // GPU friendly jitter
            float4 ScreenSnap(float4 vertex)
            {
                float geoRes = _VertJitter * 125.0 + 1.0;

                float2 pixelPos =
                    round((vertex.xy / vertex.w) *
                    _ScreenParams.xy * rcp(max(geoRes,0.00001)))
                    * geoRes;

                vertex.xy =
                    pixelPos / _ScreenParams.xy * vertex.w;

                return vertex;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos =
                    TransformObjectToWorld(IN.positionOS.xyz);

                float3 normalWS =
                    TransformObjectToWorldNormal(IN.normalOS);

                float4 clipPos =
                    TransformWorldToHClip(worldPos);

                if (_VertJitter > 0)
                    clipPos = ScreenSnap(clipPos);

                OUT.positionCS = clipPos;
                OUT.positionWS = worldPos;
                OUT.normalWS = normalWS;

                float wVal = clipPos.w;

                OUT.uv = IN.uv;
                OUT.uv_affine =
                    float3(IN.uv * wVal, wVal);

                OUT.shadowCoord =
                    TransformWorldToShadowCoord(worldPos);

                OUT.fogFactor =
                    ComputeFogFactor(clipPos.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 correctUV =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                float invW = rcp(max(IN.uv_affine.z, 0.00001));
                float2 affineUV =
                    IN.uv_affine.xy * invW;

                float2 finalUV =
                    lerp(correctUV,
                         affineUV,
                         _AffineMapIntensity);

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        finalUV
                    ) * _Color;

                Light mainLight =
                    GetMainLight(IN.shadowCoord);

                float3 normalWS =
                    normalize(IN.normalWS);

                half NdotL =
                    saturate(dot(normalWS,
                                 mainLight.direction));

                half shadow =
                    lerp(1.0,
                         mainLight.shadowAttenuation,
                         _ReceiveShadows);

                float lighting =
                    NdotL * shadow + 0.25;

                tex.rgb *= lighting;

                tex.rgb =
                    lerp(tex.rgb,
                         _HitColor.rgb,
                         _HitPower);

                tex.rgb =
                    MixFog(tex.rgb, IN.fogFactor);

                return tex;
            }

            ENDHLSL
        }
    }

    FallBack Off
}