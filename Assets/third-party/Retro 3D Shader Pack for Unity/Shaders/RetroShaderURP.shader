Shader "Retro 3D Shader Pack/Retro_Lit_URP_Final"
{
    Properties
    {
        _MainTex("Albedo Texture", 2D) = "white" {}
        _Color("Color Tint", Color) = (1,1,1,1)
        [Toggle(_EMISSION_ON)]_EmissionEnabled("Emission", Float) = 0
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "black" {}

        [Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc("Blend mode Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendDst("Blend mode Destination", Float) = 0
        [Enum(Off, 0, On, 1)]_ZWrite("Depth Write", Float) = 1

        _VertJitter("Vertex Jitter", Range(0,1)) = 0
        _AffineMapIntensity("Affine Mapping", Range(0,1)) = 1
        _DrawDist("Draw Distance", Float) = 0

        _ReceiveShadows("Receive Shadows", Range(0,1)) = 1
        _AmbientStrength("Ambient Strength", Range(0,2)) = 1
        _DirectLightStrength("Directional Light Strength", Range(0,2)) = 1
        _MinLight("Minimum Light", Range(0,1)) = 0.35

        _HitColor("Hit Color", Color) = (1,0,0,1)
        _HitPower("Hit Power", Range(0,1)) = 0

        // Fade
        [Toggle(_FADE_ON)]_FadeOn("Fade", Float) = 0
        _FadeTex("Fade Tex", 2D) = "white" {}
        _FadeAmount("Fade Amount", Range(0, 1)) = 0.0
        _FadePower("Fade Power", Range(0.25, 4.0)) = 1.0
        _FadeTransition("Fade Transition", Range(0, 0.4)) = 0.2
        [Toggle(_FADE_BURN_ON)]_FadeBurnOn("Use Fade Burn Color?", Float) = 0.0
        [HDR]_FadeBurnColor("Fade Burn Color", Color) = (1,1,0,1)
        _FadeBurnWidth("Fade Burn Width", Range(0, 0.2)) = 0.01
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
            Blend [_BlendSrc] [_BlendDst]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local _FADE_ON
            #pragma shader_feature_local _FADE_BURN_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_FadeTex);
            SAMPLER(sampler_FadeTex);

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_ST;
            float4 _Color;
            float _BlendSrc;
            float _BlendDst;
            float _ZWrite;

            float _VertJitter;
            float _AffineMapIntensity;
            float _DrawDist;
            float _ReceiveShadows;
            float _AmbientStrength;
            float _DirectLightStrength;
            float _MinLight;

            float4 _HitColor;
            float _HitPower;

            float4 _FadeTex_ST;
            float _FadeAmount;
            float _FadePower;
            float _FadeTransition;
            float4 _FadeBurnColor;
            float _FadeBurnWidth;

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

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    OUT.shadowCoord = ComputeScreenPos(clipPos);
                #else
                    OUT.shadowCoord = TransformWorldToShadowCoord(worldPos);
                #endif

                OUT.fogFactor =
                    ComputeFogFactor(clipPos.z);

                return OUT;
            }

            #if defined(_FADE_ON)
            half4 ApplyFade(half4 inputColor, float2 uv)
            {
                half4 res = inputColor;

                float2 fadeUV =
                    TRANSFORM_TEX(uv, _FadeTex);

                float fadeSample =
                    SAMPLE_TEXTURE2D(
                        _FadeTex,
                        sampler_FadeTex,
                        fadeUV
                    ).r;

                fadeSample =
                    pow(saturate(fadeSample), _FadePower);

                #if defined(_FADE_BURN_ON)
                    float fadeAmount =
                        lerp(_FadeAmount - _FadeTransition - _FadeBurnWidth,
                             1.0,
                             _FadeAmount);

                    float fade =
                        smoothstep(fadeAmount,
                                   fadeAmount + _FadeTransition,
                                   fadeSample);

                    float fadePlusBurn =
                        smoothstep(fadeAmount + _FadeBurnWidth,
                                   fadeAmount + _FadeBurnWidth + _FadeTransition,
                                   fadeSample);

                    float diff =
                        saturate(fade - fadePlusBurn);

                    res.rgb += diff * _FadeBurnColor.rgb;
                #else
                    float fadeAmount =
                        lerp(_FadeAmount - _FadeTransition,
                             1.0,
                             _FadeAmount);

                    float fade =
                        smoothstep(fadeAmount,
                                   fadeAmount + _FadeTransition,
                                   fadeSample);
                #endif

                res.a *= fade;

                return res;
            }
            #endif

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
                                 mainLight.direction) * 0.5 + 0.5);

                half shadow =
                    lerp(1.0,
                         mainLight.shadowAttenuation,
                         _ReceiveShadows);

                half3 ambient =
                    max(SampleSH(normalWS) * _AmbientStrength,
                        half3(_MinLight, _MinLight, _MinLight));

                half3 directLighting =
                    mainLight.color *
                    (NdotL *
                     shadow *
                     mainLight.distanceAttenuation *
                     _DirectLightStrength);

                tex.rgb *= ambient + directLighting;

                tex.rgb =
                    lerp(tex.rgb,
                         _HitColor.rgb,
                         _HitPower);

                #if defined(_EMISSION_ON)
                    half3 emission =
                        SAMPLE_TEXTURE2D(
                            _EmissionMap,
                            sampler_EmissionMap,
                            finalUV
                        ).rgb;

                    tex.rgb += emission;
                #endif

                #if defined(_FADE_ON)
                    tex = ApplyFade(tex, finalUV);
                    clip(tex.a - 0.001);
                #endif

                tex.rgb =
                    MixFog(tex.rgb, IN.fogFactor);

                return tex;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
