Shader "Retro 3D Shader Pack/Retro_Lit_URP_Final"
{
    Properties
    {
        _MainTex("Albedo Texture", 2D) = "white" {}
        _Color("Color Tint", Color) = (1,1,1,1)

        [Toggle(ENABLE_SCREENSPACE_JITTER)]
        _ScreenJitter("Enable Screen Jitter", Float) = 1

        _VertJitter("Vertex Jitter", Range(0,1)) = 0
        _AffineMapIntensity("Affine Mapping", Range(0,1)) = 1
        _DrawDist("Draw Distance", Float) = 0

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Width", Float) = 1


        _ReceiveShadows("Receive Shadows", Range(0,1)) = 1

        // HIT
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



        // ======================================================
        // MAIN PASS
        // ======================================================
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma shader_feature_local ENABLE_SCREENSPACE_JITTER

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
                float3 uv_affine : TEXCOORD1;
                float drawDistClip : TEXCOORD2;

                float3 normalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;

                half fogFactor : TEXCOORD6;
                half3 vertexLight : TEXCOORD7;
            };

            float4 ScreenSnap(float4 vertex)
            {
                float geoRes = _VertJitter * 125.0 + 1.0;
                float2 pixelPos =
                    round((vertex.xy / vertex.w) * _ScreenParams.xy / geoRes)
                    * geoRes;

                vertex.xy = pixelPos / _ScreenParams.xy * vertex.w;
                return vertex;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float4 clipPos = TransformWorldToHClip(worldPos);

                #ifdef ENABLE_SCREENSPACE_JITTER
                clipPos = ScreenSnap(clipPos);
                #endif

                OUT.positionCS = clipPos;
                OUT.positionWS = worldPos;
                OUT.normalWS = normalWS;

                // LEGACY AFFINE
                float4 reproj = mul(UNITY_MATRIX_P, clipPos);
                float wVal = reproj.z;

                OUT.uv = IN.uv;
                OUT.uv_affine = float3(IN.uv * wVal, wVal);

                // Draw distance
                OUT.drawDistClip = 0;
                if (_DrawDist != 0 &&
                    distance(worldPos, _WorldSpaceCameraPos) > _DrawDist)
                    OUT.drawDistClip = 1;

                OUT.shadowCoord = TransformWorldToShadowCoord(worldPos);

                OUT.vertexLight = VertexLighting(worldPos, normalWS);

                OUT.fogFactor = ComputeFogFactor(clipPos.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (IN.drawDistClip != 0)
                    clip(-1);

                float2 correctUV =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                float2 affineUV =
                    TRANSFORM_TEX(
                        (IN.uv_affine.xy / IN.uv_affine.z),
                        _MainTex
                    );

                float2 finalUV =
                    lerp(correctUV, affineUV, _AffineMapIntensity);

                half4 tex =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV)
                    * _Color;

                // ===== Lighting =====

                Light mainLight =
                    GetMainLight(IN.shadowCoord);

                half NdotL =
                    saturate(dot(IN.normalWS, mainLight.direction));

                half3 lighting =
                    mainLight.color * NdotL;

                lighting *=
                    lerp(1.0,
                         mainLight.shadowAttenuation,
                         _ReceiveShadows);

                lighting += IN.vertexLight;

                tex.rgb *= lighting;

                // ===== HIT EFFECT =====
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

        Pass
        {
            Name "OutlineRenderObjects"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityDOTSInstancing.hlsl"

            // Needed to support the GPU resident drawer. 
            // Note that I have removed stuff that it seems I do not need.
            // See https://gamedev.center/how-to-write-a-custom-urp-shader-with-dots-instancing-support/
            #pragma target 4.5

            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex Vert
            #pragma fragment Frag

            float4 _OutlineMaskColor;

            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                return OUT;
            }

            float4 Frag() : SV_Target
            {
                return _OutlineMaskColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "OutlineHorizontalBlur"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
            ColorMask RGBA

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./GaussianBlur.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            int _BlurKernelRadius;
            float _BlurStandardDeviation;

            float4 Frag(Varyings input) : SV_Target
            {
                float scale = ((float)_ScreenParams.y / 1440.0);

                return GaussianBlur(input.texcoord, float2(1.0, 0.0), _BlurKernelRadius, _BlurStandardDeviation,
                                    _BlitTexture, sampler_LinearClamp, _BlitTexture_TexelSize.xy * scale);
            }
            ENDHLSL
        }

        Pass
        {
            Name "OutlineVerticalBlur"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
            ColorMask RGBA

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./GaussianBlur.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            int _BlurKernelRadius;
            float _BlurStandardDeviation;

            float4 Frag(Varyings input) : SV_Target
            {
                float scale = ((float)_ScreenParams.y / 1440.0);

                return GaussianBlur(input.texcoord, float2(0.0, 1.0), _BlurKernelRadius, _BlurStandardDeviation,
                                    _BlitTexture, sampler_LinearClamp, _BlitTexture_TexelSize.xy * scale);
            }
            ENDHLSL
        }

        Pass
        {
            Name "OutlineResolve"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D_X(_OutlineRenderedObjectsMaskTexture);
            TEXTURE2D_X(_OutlineBlurredRenderedObjectsMaskTexture);

            SAMPLER(sampler_BlitTexture);

            float4 _OutlineColors[] =
            {
                float4(1.0, 1.0, 1.0, 1.0),
                float4(1.0, 1.0, 1.0, 1.0),
                float4(1.0, 1.0, 1.0, 1.0),
                float4(1.0, 1.0, 1.0, 1.0),
            };
            float4 _OutlineFallOffs;
            float4 _FillAlphas;

            float4 Remap4(float4 origFrom, float4 origTo, float4 targetFrom, float4 targetTo, float4 value)
            {
                return lerp(targetFrom, targetTo, (value - origFrom) / (origTo - origFrom));
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // get the camera color, the original objects render mask, and the expanded (blurred) mask
                float4 cameraColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord);
                float4 mask = SAMPLE_TEXTURE2D_X(_OutlineRenderedObjectsMaskTexture, sampler_PointClamp,
                                    input.texcoord);
                float4 blurredMask = SAMPLE_TEXTURE2D_X(_OutlineBlurredRenderedObjectsMaskTexture, sampler_LinearClamp,
                                                                               input.texcoord);

                // subtract the original mask to the blurred mask to resolve the outline
                float4 outlineAlphas = saturate(blurredMask - mask);

                // apply some sort of softness to the outline
                float4 outlineAlphaRemap = Remap4(0.0, _OutlineFallOffs, 0.0, 1.0, outlineAlphas);
                outlineAlphas = outlineAlphas > 0.0
     ? (outlineAlphas > _OutlineFallOffs ? 1.0 : outlineAlphaRemap)
     : outlineAlphas;

                // alpha can be used to fade out the final outline
                outlineAlphas *= float4(_OutlineColors[0].a, _OutlineColors[1].a, _OutlineColors[2].a,
                          _OutlineColors[3].a);

                // if mask is greater than 1.0, then use the fill alpha, otherwise keep the outline alpha
                outlineAlphas = lerp(outlineAlphas, _FillAlphas, step(1.0, mask));

                // calculate the maximum alpha for when several layers intersect on screen
                float maxAlpha = Max3(outlineAlphas.x, outlineAlphas.y, max(outlineAlphas.z, outlineAlphas.w));

                // calculate each layer color individually
                float3 layer1Color = _OutlineColors[0].rgb * step(0.0, blurredMask.r) * outlineAlphas.r;
                float3 layer2Color = _OutlineColors[1].rgb * step(0.0, blurredMask.g) * outlineAlphas.g;
                float3 layer3Color = _OutlineColors[2].rgb * step(0.0, blurredMask.b) * outlineAlphas.b;
                float3 layer4Color = _OutlineColors[3].rgb * step(0.0, blurredMask.a) * outlineAlphas.a;

                // calculate the total color for when the layers overlap
                float3 layersSumColor = layer1Color + layer2Color + layer3Color + layer4Color;

                // blend the color with the background 
                float3 composedColor = cameraColor.rgb * (1.0 - maxAlpha) + (layersSumColor * maxAlpha);

                return float4(composedColor, cameraColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}