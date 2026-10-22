Shader "Little Rush/UI/TMP Bitmap Pixel Outline"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceTex ("Font Texture", 2D) = "white" {}
        _FaceColor ("Text Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        [IntRange] _OutlineWidth ("Outline Width (Atlas Pixels)", Range(0, 16)) = 2
        [Toggle(BITMAP_SHADOW_ON)] _UseShadow ("Use Shadow", Float) = 0
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0)
        _ShadowOffsetX ("Shadow Offset X (Atlas Pixels)", Range(-32, 32)) = 0
        _ShadowOffsetY ("Shadow Offset Y (Atlas Pixels)", Range(-32, 32)) = 0
        [IntRange] _ShadowLength ("Shadow Length (Atlas Pixels)", Range(0, 64)) = 4
        [IntRange] _ShadowWidth ("Shadow Width (Atlas Pixels)", Range(0, 16)) = 3
        [HideInInspector] _Padding ("Padding", Float) = 4

        _VertexOffsetX ("Vertex Offset X", Float) = 0
        _VertexOffsetY ("Vertex Offset Y", Float) = 0
        _MaskSoftnessX ("Mask Softness X", Float) = 0
        _MaskSoftnessY ("Mask Softness Y", Float) = 0

        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector] _CullMode ("Cull Mode", Float) = 0
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use UI Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Lighting Off
        Cull [_CullMode]
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local __ BITMAP_SHADOW_ON

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 mask : TEXCOORD2;
                float2 glyphCoord : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _FaceTex;
            float4 _FaceTex_ST;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            fixed4 _ShadowColor;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            float _ShadowLength;
            float _ShadowWidth;
            float _Padding;

            float _VertexOffsetX;
            float _VertexOffsetY;
            float4 _ClipRect;
            float _MaskSoftnessX;
            float _MaskSoftnessY;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t input)
            {
                float4 position = input.vertex;
                position.x += _VertexOffsetX;
                position.y += _VertexOffsetY;
                position.xy += (position.w * 0.5) / _ScreenParams.xy;

                float4 clipPosition = UnityPixelSnap(UnityObjectToClipPos(position));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    input.color.rgb = UIGammaToLinear(input.color.rgb);

                v2f output;
                output.vertex = clipPosition;
                output.color = input.color * _FaceColor;
                output.texcoord0 = input.texcoord0.xy;
                output.texcoord1 = TRANSFORM_TEX(input.texcoord1, _FaceTex);
                output.glyphCoord = input.texcoord1;

                float2 pixelSize = clipPosition.w;
                pixelSize /= abs(float2(
                    _ScreenParams.x * UNITY_MATRIX_P[0][0],
                    _ScreenParams.y * UNITY_MATRIX_P[1][1]));

                const float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                const half2 maskSoftness = half2(
                    max(_UIMaskSoftnessX, _MaskSoftnessX),
                    max(_UIMaskSoftnessY, _MaskSoftnessY));
                output.mask = float4(
                    position.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * maskSoftness + pixelSize.xy));

                return output;
            }

            fixed SampleGlyphAlpha(float2 uv, float4 glyphBounds)
            {
                if (any(uv < glyphBounds.xy) || any(uv >= glyphBounds.zw))
                    return 0;

                // Explicit LOD keeps sampling valid inside the variable shadow loops.
                return tex2Dlod(_MainTex, float4(uv, 0, 0)).a;
            }

            #if BITMAP_SHADOW_ON
                fixed SampleShadowAlpha(float2 uv, float4 glyphBounds)
                {
                    const float2 offset = float2(_ShadowOffsetX, _ShadowOffsetY);
                    const float offsetLength = length(offset);
                    const float2 direction = offsetLength > 0.0001
                        ? offset / offsetLength
                        : float2(0.70710678, -0.70710678);
                    const int shadowLength = (int)floor(clamp(_ShadowLength, 0, 64) + 0.5);
                    const int shadowRadius = (int)floor(clamp(_ShadowWidth, 0, 16) + 0.5);
                    const float2 endOffset = direction * shadowLength;
                    const int2 minimum = (int2)floor(min(float2(0, 0), endOffset)) - shadowRadius;
                    const int2 maximum = (int2)ceil(max(float2(0, 0), endOffset)) + shadowRadius;
                    const float2 safeDirection = float2(
                        direction.x < 0 ? min(direction.x, -0.0001) : max(direction.x, 0.0001),
                        direction.y < 0 ? min(direction.y, -0.0001) : max(direction.y, 0.0001));
                    const float2 inverseDirection = 1.0 / safeDirection;
                    const float radius = shadowRadius + 0.5;
                    fixed shadowAlpha = 0;

                    // Sample the swept square once per atlas pixel, rather than
                    // repeating the entire width filter at every length step.
                    for (int y = minimum.y; y <= maximum.y; y++)
                    {
                        for (int x = minimum.x; x <= maximum.x; x++)
                        {
                            const float2 sampleOffset = float2(x, y);
                            const float2 first = (sampleOffset - radius) * inverseDirection;
                            const float2 last = (sampleOffset + radius) * inverseDirection;
                            const float2 entry = min(first, last);
                            const float2 exit = max(first, last);
                            const float entryDistance = max(0, max(entry.x, entry.y));
                            const float exitDistance = min(shadowLength, min(exit.x, exit.y));

                            if (entryDistance <= exitDistance)
                            {
                                const float2 sampleUv = uv -
                                    _MainTex_TexelSize.xy * (offset + sampleOffset);
                                shadowAlpha = max(shadowAlpha, SampleGlyphAlpha(sampleUv, glyphBounds));
                                if (shadowAlpha >= 1)
                                    return shadowAlpha;
                            }
                        }
                    }

                    return shadowAlpha;
                }
            #endif

            fixed4 frag(v2f input) : SV_Target
            {
                const float2 uv = input.texcoord0;
                // TMP Character texture mapping supplies 0..1 coordinates per quad.
                // Remove its bitmap padding to isolate this glyph from atlas neighbours.
                const float2 uvSpan = fwidth(uv) / max(fwidth(input.glyphCoord), float2(0.000001, 0.000001));
                const float2 quadMinimum = uv - input.glyphCoord * uvSpan;
                const float2 glyphPadding = (floor(_Padding) + 1) * _MainTex_TexelSize.xy;
                const float4 glyphBounds = float4(
                    quadMinimum + glyphPadding,
                    quadMinimum + uvSpan - glyphPadding);
                const fixed centerAlpha = SampleGlyphAlpha(uv, glyphBounds);
                const int outlineRadius = (int)floor(clamp(_OutlineWidth, 0, 16) + 0.5);

                fixed expandedAlpha = centerAlpha;

                // Square max filter. Every atlas pixel inside the selected radius is
                // sampled, so widths above one remain solid instead of forming gaps.
                for (int y = -outlineRadius; y <= outlineRadius; y++)
                {
                    for (int x = -outlineRadius; x <= outlineRadius; x++)
                    {
                        if (x != 0 || y != 0)
                        {
                            const float2 sampleOffset = _MainTex_TexelSize.xy * float2(x, y);
                            expandedAlpha = max(expandedAlpha, SampleGlyphAlpha(uv + sampleOffset, glyphBounds));
                        }
                    }
                }

                const fixed faceAlpha = centerAlpha * input.color.a;
                const fixed outlineAlpha = saturate(expandedAlpha - centerAlpha) * _OutlineColor.a * input.color.a;
                fixed outputAlpha = faceAlpha + outlineAlpha * (1 - faceAlpha);

                const fixed3 faceColor = tex2D(_FaceTex, input.texcoord1).rgb * input.color.rgb;
                fixed3 premultipliedColor =
                    faceColor * faceAlpha + _OutlineColor.rgb * outlineAlpha * (1 - faceAlpha);

                #if BITMAP_SHADOW_ON
                    if (_ShadowColor.a > 0 && outputAlpha < 1)
                    {
                        const fixed shadowAlpha = SampleShadowAlpha(uv, glyphBounds) *
                            _ShadowColor.a * input.color.a * (1 - outputAlpha);
                        premultipliedColor += _ShadowColor.rgb * shadowAlpha;
                        outputAlpha += shadowAlpha;
                    }
                #endif

                fixed4 color = fixed4(
                    premultipliedColor / max(outputAlpha, 0.0001),
                    outputAlpha);

                #if UNITY_UI_CLIP_RECT
                    const half2 mask = saturate(
                        (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                    color *= mask.x * mask.y;
                #endif

                #if UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    CustomEditor "LittleRush.Editor.BitmapPixelOutlineShaderGUI"
}
