Shader "Custom/D20MotionBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _BlurAmount ("Blur Amount", Range(0, 1)) = 0.0
        _BlurDir ("Blur Direction (XY)", Vector) = (1.0, 0.5, 0, 0)
        _BlurScale ("Blur Scale", Range(0.002, 0.04)) = 0.012
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _BlurAmount;
            float4 _BlurDir;
            float _BlurScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Освещение граней
                float3 lightDir = normalize(float3(0.5, 1.0, -0.5));
                float NdotL = max(0.35, dot(i.worldNormal, lightDir));

                // РЕЖИМ ПОКОЯ: Блюр полностью выключен
                if (_BlurAmount <= 0.001)
                {
                    fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color;
                    return baseCol * NdotL;
                }

                // РЕЖИМ ВРАЩЕНИЯ: Вычисляем сдвиг текстуры
                float2 dir = float2(1.0, 0.5);
                if (length(_BlurDir.xy) > 0.001)
                {
                    dir = normalize(_BlurDir.xy);
                }

                float stepSize = _BlurAmount * _BlurScale;
                fixed4 accumulatedColor = fixed4(0, 0, 0, 0);

                const int SAMPLES = 9;
                for (int j = -4; j <= 4; j++)
                {
                    float2 offsetUV = i.uv + dir * ((float)j * stepSize);
                    fixed4 sampleCol = tex2Dlod(_MainTex, float4(offsetUV, 0, 0));
                    accumulatedColor += sampleCol;
                }

                fixed4 finalTex = (accumulatedColor / SAMPLES) * _Color;
                return finalTex * NdotL;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}