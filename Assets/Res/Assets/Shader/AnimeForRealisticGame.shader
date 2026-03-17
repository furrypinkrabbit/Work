Shader "Custom/AnimeForRealisticGame"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.7,0.5,0.55,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.02)) = 0.005
        _LightStep ("Light Step", Range(0.1, 0.9)) = 0.5
        _Smoothness ("Smoothness", Range(1,10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShadowColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _LightStep;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = normalize(i.normal);
                float NdotL = dot(normal, lightDir);
                NdotL = saturate(NdotL);
                float light = smoothstep(_LightStep, _LightStep + 0.05, NdotL);
                fixed3 finalColor = lerp(_ShadowColor.rgb, col.rgb, light);

                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float NdotV = dot(normal, viewDir);
                float outline = 1 - saturate(NdotV);
                outline = step(0.85, outline);
                finalColor = lerp(finalColor, _OutlineColor.rgb, outline * 0.3);

                return fixed4(finalColor, 1);
            }
            ENDCG
        }

        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            float4 vert (appdata v) : SV_POSITION
            {
                float4 pos = UnityObjectToClipPos(v.vertex);
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                pos.xy += viewNormal.xy * _OutlineWidth * 0.05;
                return pos;
            }

            float4 frag () : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}