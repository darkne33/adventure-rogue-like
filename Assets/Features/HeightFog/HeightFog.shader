Shader "Hidden/Little Rush/Height Fog"
{
    Properties
    {
        [HDR] _FogColor ("Fog Color", Color) = (0.42, 0.52, 0.62, 1)
        _Intensity ("Overall Intensity", Range(0, 2)) = 1

        _StartDistance ("Start Distance", Float) = 8
        _EndDistance ("Full Fog Distance", Float) = 65

        _BottomHeight ("Bottom Fade Height (World Y)", Float) = 0
        _TopHeight ("Top Fade Height (World Y)", Float) = 18
        _BottomDensity ("Bottom Density", Range(0, 1)) = 0.08
        _TopDensity ("Top Density", Range(0, 1)) = 1

        _BottomMaxOpacity ("Bottom Max Opacity", Range(0, 1)) = 0.75
        _TopMaxOpacity ("Top Max Opacity", Range(0, 1)) = 0.75
        [Toggle] _AffectSky ("Affect Sky", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Height Fog"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _Intensity;
                float _StartDistance;
                float _EndDistance;
                float _BottomHeight;
                float _TopHeight;
                float _BottomDensity;
                float _TopDensity;
                float _BottomMaxOpacity;
                float _TopMaxOpacity;
                float _AffectSky;
            CBUFFER_END

            float Smooth01(float value)
            {
                value = saturate(value);
                return value * value * (3.0 - 2.0 * value);
            }

            float GetHeightFade(float worldY)
            {
                float heightRange = max(_TopHeight - _BottomHeight, 0.0001);
                return Smooth01((worldY - _BottomHeight) / heightRange);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 sourceColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv);

                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    float isSky = 1.0 - step(0.0001, rawDepth);
                #else
                    float isSky = step(0.9999, rawDepth);
                #endif

                if (_AffectSky < 0.5 && isSky > 0.5)
                    return sourceColor;

                float deviceDepth = rawDepth;
                #if !UNITY_REVERSED_Z
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                #endif

                float3 positionWS = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 cameraPositionWS = GetCameraPositionWS();
                float distanceToCamera = distance(cameraPositionWS, positionWS);

                float distanceRange = max(_EndDistance - _StartDistance, 0.0001);
                float distanceFade = Smooth01((distanceToCamera - _StartDistance) / distanceRange);

                float heightFade = GetHeightFade(positionWS.y);
                float heightDensity = lerp(_BottomDensity, _TopDensity, heightFade);
                float maxOpacity = lerp(_BottomMaxOpacity, _TopMaxOpacity, heightFade);

                float fogAmount = saturate(distanceFade * heightDensity * _Intensity);
                fogAmount *= maxOpacity;

                half3 foggedColor = lerp(sourceColor.rgb, _FogColor.rgb, fogAmount);
                return half4(foggedColor, sourceColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
