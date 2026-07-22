Shader "Custom/UI/Shine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _ShineColor ("Shine Color", Color) = (1,1,1,1)

        _ShineWidth ("Width", Range(0.01,1)) = 0.18

        _ShineSoftness ("Softness", Range(0.01,1)) = 0.12

        _ShinePosition ("Position", Range(-2,2)) = -1

        _Intensity ("Intensity", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _MainTex_ST;

            float4 _ShineColor;

            float _ShineWidth;
            float _ShineSoftness;
            float _ShinePosition;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex,i.uv);

                float diag = i.uv.x + i.uv.y;

                float d = abs(diag - _ShinePosition);

                float shine =
                    smoothstep(
                        _ShineWidth + _ShineSoftness,
                        _ShineWidth,
                        d);

                col.rgb += _ShineColor.rgb * shine * _Intensity;

                return col;
            }

            ENDCG
        }
    }
}