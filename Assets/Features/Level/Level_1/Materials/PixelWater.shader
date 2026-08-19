Shader "Little Rush/Environment/PixelWater"
{
    Properties
    {
        [HDR] _DeepColor ("Deep Water", Color) = (0.005, 0.09, 0.42, 1)
        [HDR] _MidColor ("Mid Water", Color) = (0.0, 0.46, 1.15, 1)
        [HDR] _LightColor ("Light Water", Color) = (0.02, 1.15, 2.1, 1)
        [HDR] _FoamColor ("Foam / Glints", Color) = (1.1, 2.35, 3.0, 1)
        [HDR] _ContactFoamColor ("Contact Foam", Color) = (0.22, 1.65, 2.5, 1)

        _PatternScale ("Pattern Scale", Range(0.05, 2.0)) = 0.34
        _PixelSize ("World Pixel Size", Range(0.01, 1.0)) = 0.16
        _FlowSpeed ("Flow Speed", Range(0.0, 3.0)) = 0.42
        _FlowDirection ("Flow Direction (X Z)", Vector) = (1.0, 0.28, 0.0, 0.0)
        _PatternOffset ("Pattern Offset (X Z)", Vector) = (0.0, 0.0, 0.0, 0.0)
        _WarpStrength ("Flow Distortion", Range(0.0, 3.0)) = 1.35

        _BandOne ("Mid Color Border", Range(0.0, 1.0)) = 0.38
        _BandTwo ("Light Color Border", Range(0.0, 1.0)) = 0.68
        _FoamWidth ("Foam Line Width", Range(0.002, 0.2)) = 0.035
        _FoamAmount ("Foam Amount", Range(0.0, 2.0)) = 0.9
        _SparkleAmount ("Pixel Glints", Range(0.0, 2.0)) = 0.75
        _Glow ("Emission Strength", Range(0.0, 4.0)) = 1.2

        _ContactFoamDistance ("Contact Foam Width", Range(0.01, 3.0)) = 0.55
        _ContactFoamSharpness ("Contact Foam Sharpness", Range(0.25, 8.0)) = 2.2
        _ContactFoamBreakup ("Contact Foam Breakup", Range(0.0, 1.0)) = 0.55
        _ContactFoamNoiseScale ("Contact Foam Noise Scale", Range(0.1, 10.0)) = 2.1
        _ContactFoamPixelSize ("Contact Foam Pixel Size", Range(1.0, 8.0)) = 2.0
        _ContactFoamStrength ("Contact Foam Strength", Range(0.0, 2.0)) = 1.0

        _WaveHeight ("Geometry Wave Height", Range(0.0, 0.5)) = 0.0
        _WaveScale ("Geometry Wave Scale", Range(0.05, 5.0)) = 0.72
        _WaveSpeed ("Geometry Wave Speed", Range(0.0, 5.0)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend One Zero

        Pass
        {
            Name "PixelWaterUnlit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _MidColor;
                half4 _LightColor;
                half4 _FoamColor;
                half4 _ContactFoamColor;
                float4 _FlowDirection;
                float4 _PatternOffset;
                float _PatternScale;
                float _PixelSize;
                float _FlowSpeed;
                float _WarpStrength;
                float _BandOne;
                float _BandTwo;
                float _FoamWidth;
                float _FoamAmount;
                float _SparkleAmount;
                float _Glow;
                float _ContactFoamDistance;
                float _ContactFoamSharpness;
                float _ContactFoamBreakup;
                float _ContactFoamNoiseScale;
                float _ContactFoamPixelSize;
                float _ContactFoamStrength;
                float _WaveHeight;
                float _WaveScale;
                float _WaveSpeed;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), local.x);
                float top = lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), local.x);
                return lerp(bottom, top, local.y);
            }

            float Fbm(float2 p)
            {
                const float2x2 rotation = float2x2(0.8, -0.6, 0.6, 0.8);
                float value = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(p) * amplitude;
                    p = mul(rotation, p) * 2.03 + 17.17;
                    amplitude *= 0.5;
                }

                return value;
            }

            float2 SafeFlowDirection()
            {
                float2 direction = _FlowDirection.xy;
                return direction * rsqrt(max(dot(direction, direction), 0.0001));
            }

            float WaterField(float2 worldXZ, float timeValue, float2 flowDirection)
            {
                float2 p = worldXZ * _PatternScale;
                float2 crossDirection = float2(-flowDirection.y, flowDirection.x);

                float warpX = Fbm(p * 0.62 + flowDirection * timeValue * 0.21);
                float warpY = Fbm(p * 0.62 + 8.31 - crossDirection * timeValue * 0.17);
                float2 warp = (float2(warpX, warpY) - 0.5) * _WarpStrength;

                float broadFlow = Fbm(p + warp + flowDirection * timeValue);
                float longStreaks = Fbm(
                    float2(p.x * 0.58, p.y * 1.48) +
                    warp * 0.72 -
                    crossDirection * timeValue * 0.48
                );

                return saturate(broadFlow * 0.72 + longStreaks * 0.28);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float waveTime = _Time.y * _WaveSpeed;
                float waveA = sin(dot(positionWS.xz, float2(0.86, 0.50)) * _WaveScale + waveTime);
                float waveB = sin(dot(positionWS.xz, float2(-0.36, 0.93)) * (_WaveScale * 1.37) - waveTime * 1.21);
                positionWS.y += (waveA + waveB) * 0.5 * _WaveHeight;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float pixelSize = max(_PixelSize, 0.0001);
                float2 patternPosition = input.positionWS.xz + _PatternOffset.xy;
                patternPosition = (floor(patternPosition / pixelSize) + 0.5) * pixelSize;

                float timeValue = _Time.y * _FlowSpeed;
                float2 flowDirection = SafeFlowDirection();
                float field = WaterField(patternPosition, timeValue, flowDirection);
                float antialiasing = max(fwidth(field) * 0.55, 0.002);

                float midBand = smoothstep(_BandOne - antialiasing, _BandOne + antialiasing, field);
                float lightBand = smoothstep(_BandTwo - antialiasing, _BandTwo + antialiasing, field);

                half3 waterColor = lerp(_DeepColor.rgb, _MidColor.rgb, midBand);
                waterColor = lerp(waterColor, _LightColor.rgb, lightBand);

                float detail = Fbm(patternPosition * (_PatternScale * 2.7) - flowDirection * timeValue * 1.35);
                float brokenFoam = smoothstep(0.42, 0.67, detail);
                float foamDistance = abs(field - _BandTwo);
                float foam = 1.0 - smoothstep(_FoamWidth, _FoamWidth + antialiasing * 1.5, foamDistance);
                foam *= brokenFoam * _FoamAmount;

                float2 sparkleCell = floor(patternPosition * (_PatternScale * 3.2) + flowDirection * timeValue * 0.85);
                float sparkleRandom = Hash21(sparkleCell);
                float sparklePulse = 0.5 + 0.5 * sin(timeValue * 5.0 + sparkleRandom * 6.2831853);
                float sparkle = step(0.91, sparkleRandom) * smoothstep(0.68, 0.92, sparklePulse);
                sparkle *= smoothstep(_BandOne, _BandTwo, field) * _SparkleAmount;

                float highlight = saturate(foam + sparkle);
                waterColor = lerp(waterColor, _FoamColor.rgb, highlight);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 contactPixelSize = max(_ContactFoamPixelSize, 1.0) / _ScaledScreenParams.xy;
                screenUV = (floor(screenUV / contactPixelSize) + 0.5) * contactPixelSize;

                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float waterEyeDepth = -TransformWorldToView(input.positionWS).z;
                float depthDifference = max(sceneEyeDepth - waterEyeDepth, 0.0);

                float contactFoam = saturate(1.0 - depthDifference / max(_ContactFoamDistance, 0.0001));
                contactFoam = pow(contactFoam, _ContactFoamSharpness);

                float contactNoise = Fbm(
                    patternPosition * _ContactFoamNoiseScale -
                    flowDirection * timeValue * 0.35
                );
                float brokenContact = smoothstep(0.28, 0.68, contactNoise + contactFoam * 0.38);
                contactFoam *= lerp(1.0, brokenContact, _ContactFoamBreakup);
                contactFoam = saturate(contactFoam * _ContactFoamStrength);

                waterColor = lerp(waterColor, _ContactFoamColor.rgb, contactFoam);
                waterColor *= _Glow;
                waterColor = MixFog(waterColor, input.fogFactor);

                return half4(waterColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
