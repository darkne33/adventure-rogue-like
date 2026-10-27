Shader "LittleRush/PickupPixelParticle"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Enum(Square,0,Star,1)] _Shape ("Pixel Shape", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Shape;

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.texcoord = input.texcoord;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (_Shape > 0.5)
                {
                    float2 pixel = min(floor(saturate(input.texcoord) * 5.0), 4.0);
                    float arm = min(abs(pixel.x - 2.0), abs(pixel.y - 2.0));
                    clip(0.5 - arm);
                }
                return input.color;
            }
            ENDCG
        }
    }
}
