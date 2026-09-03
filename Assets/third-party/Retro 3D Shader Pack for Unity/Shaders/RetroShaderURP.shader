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
        [HDR]_AttackTelegraphColor("Attack Telegraph Color", Color) = (2,2,2,1)
        _AttackTelegraphPower("Attack Telegraph Power", Range(0,1)) = 0

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
            "UniversalMaterialType"="Lit"
            "IgnoreProjector"="True"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
        float4 _AttackTelegraphColor;
        float _AttackTelegraphPower;

        float4 _FadeTex_ST;
        float _FadeAmount;
        float _FadePower;
        float _FadeTransition;
        float4 _FadeBurnColor;
        float _FadeBurnWidth;

        CBUFFER_END

        float4 ScreenSnap(float4 positionCS)
        {
            float geoRes = _VertJitter * 125.0 + 1.0;

            float2 pixelPos =
                round((positionCS.xy / positionCS.w) *
                _ScreenParams.xy * rcp(max(geoRes, 0.00001))) *
                geoRes;

            positionCS.xy =
                pixelPos / _ScreenParams.xy * positionCS.w;

            return positionCS;
        }

        float2 GetFinalUV(float2 uv, float3 uvAffine)
        {
            float2 correctUV = TRANSFORM_TEX(uv, _MainTex);
            float invW = rcp(max(uvAffine.z, 0.00001));
            float2 affineUV = uvAffine.xy * invW;

            return lerp(correctUV, affineUV, _AffineMapIntensity);
        }

        #if defined(_FADE_ON)
        half EvaluateFade(float2 uv, out half fadeSample)
        {
            float2 fadeUV = TRANSFORM_TEX(uv, _FadeTex);
            fadeSample = SAMPLE_TEXTURE2D(_FadeTex, sampler_FadeTex, fadeUV).r;
            fadeSample = pow(saturate(fadeSample), _FadePower);

            #if defined(_FADE_BURN_ON)
                float fadeAmount = lerp(
                    _FadeAmount - _FadeTransition - _FadeBurnWidth,
                    1.0,
                    _FadeAmount);
            #else
                float fadeAmount = lerp(
                    _FadeAmount - _FadeTransition,
                    1.0,
                    _FadeAmount);
            #endif

            return smoothstep(
                fadeAmount,
                fadeAmount + _FadeTransition,
                fadeSample);
        }

        half4 ApplyFade(half4 inputColor, float2 uv)
        {
            half4 result = inputColor;
            half fadeSample;
            half fade = EvaluateFade(uv, fadeSample);

            #if defined(_FADE_BURN_ON)
                float fadeAmount = lerp(
                    _FadeAmount - _FadeTransition - _FadeBurnWidth,
                    1.0,
                    _FadeAmount);

                half fadePlusBurn = smoothstep(
                    fadeAmount + _FadeBurnWidth,
                    fadeAmount + _FadeBurnWidth + _FadeTransition,
                    fadeSample);

                result.rgb += saturate(fade - fadePlusBurn) * _FadeBurnColor.rgb;
            #endif

            result.a *= fade;
            return result;
        }
        #endif

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }

            Cull Back
            Blend [_BlendSrc] [_BlendDst]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local _FADE_ON
            #pragma shader_feature_local _FADE_BURN_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                #elif !defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    OUT.shadowCoord = TransformWorldToShadowCoord(worldPos);
                #else
                    // Cascade selection must happen per pixel. Interpolating
                    // coordinates produced by different cascade matrices causes
                    // seams and swimming on large triangles.
                    OUT.shadowCoord = 0.0;
                #endif

                OUT.fogFactor =
                    ComputeFogFactor(clipPos.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 finalUV = GetFinalUV(IN.uv, IN.uv_affine);

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        finalUV
                    ) * _Color;

                float4 shadowCoord = IN.shadowCoord;

                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #endif

                Light mainLight = GetMainLight(
                    shadowCoord,
                    IN.positionWS,
                    unity_ProbesOcclusion);

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

                tex.rgb =
                    lerp(tex.rgb,
                         _AttackTelegraphColor.rgb,
                         saturate(_AttackTelegraphPower));

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

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma shader_feature_local _FADE_ON
            #pragma shader_feature_local _FADE_BURN_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 uvAffine : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                output.uvAffine = float3(input.uv * positionCS.w, positionCS.w);
                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_FADE_ON)
                    float2 finalUV = GetFinalUV(input.uv, input.uvAffine);
                    half fadeSample;
                    clip(EvaluateFade(finalUV, fadeSample) - 0.001h);
                #endif

                return 0;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _FADE_ON
            #pragma shader_feature_local _FADE_BURN_ON

            struct DepthOnlyAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthOnlyVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 uvAffine : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthOnlyVaryings DepthOnlyVertex(DepthOnlyAttributes input)
            {
                DepthOnlyVaryings output = (DepthOnlyVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);

                if (_VertJitter > 0.0)
                    positionCS = ScreenSnap(positionCS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                output.uvAffine = float3(input.uv * positionCS.w, positionCS.w);
                return output;
            }

            half DepthOnlyFragment(DepthOnlyVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(_FADE_ON)
                    float2 finalUV = GetFinalUV(input.uv, input.uvAffine);
                    half fadeSample;
                    clip(EvaluateFade(finalUV, fadeSample) - 0.001h);
                #endif

                return input.positionCS.z;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #pragma shader_feature_local _FADE_ON
            #pragma shader_feature_local _FADE_BURN_ON

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 uvAffine : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVertex(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output = (DepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);

                if (_VertJitter > 0.0)
                    positionCS = ScreenSnap(positionCS);

                output.positionCS = positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.uvAffine = float3(input.uv * positionCS.w, positionCS.w);
                return output;
            }

            half4 DepthNormalsFragment(DepthNormalsVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(_FADE_ON)
                    float2 finalUV = GetFinalUV(input.uv, input.uvAffine);
                    half fadeSample;
                    clip(EvaluateFade(finalUV, fadeSample) - 0.001h);
                #endif

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);

                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    return half4(packedNormalWS, 0.0h);
                #else
                    return half4(normalWS, 0.0h);
                #endif
            }

            ENDHLSL
        }
    }

    FallBack Off
}
