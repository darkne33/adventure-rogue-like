Shader "Little Rush/UI/TMP Bitmap Pixel Outline"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceTex ("Font Texture", 2D) = "white" {}
        _FaceColor ("Text Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        [IntRange] _OutlineWidth ("Outline Width (Atlas Pixels)", Range(0, 4)) = 2
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
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _FaceTex;
            float4 _FaceTex_ST;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;

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

            fixed4 frag(v2f input) : SV_Target
            {
                const float2 uv = input.texcoord0;
                const fixed centerAlpha = tex2D(_MainTex, uv).a;
                const int outlineRadius = (int)floor(_OutlineWidth + 0.5);

                fixed expandedAlpha = centerAlpha;

                // Square max filter. Every atlas pixel inside the selected radius is
                // sampled, so widths above one remain solid instead of forming gaps.
                for (int y = -4; y <= 4; y++)
                {
                    for (int x = -4; x <= 4; x++)
                    {
                        const int sampleRadius = max(abs(x), abs(y));
                        if (sampleRadius > 0 && sampleRadius <= outlineRadius)
                        {
                            const float2 sampleOffset = _MainTex_TexelSize.xy * float2(x, y);
                            expandedAlpha = max(expandedAlpha, tex2D(_MainTex, uv + sampleOffset).a);
                        }
                    }
                }

                const fixed faceAlpha = centerAlpha * input.color.a;
                const fixed outlineAlpha = saturate(expandedAlpha - centerAlpha) * _OutlineColor.a * input.color.a;
                const fixed outputAlpha = faceAlpha + outlineAlpha * (1 - faceAlpha);

                const fixed3 faceColor = tex2D(_FaceTex, input.texcoord1).rgb * input.color.rgb;
                const fixed3 premultipliedColor =
                    faceColor * faceAlpha + _OutlineColor.rgb * outlineAlpha * (1 - faceAlpha);

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
}
