Shader "Custom/PurifySphereOnModels"
{
    Properties
    {
        _MainTex ("BaseTex", 2D) = "white" {}
        _LineColor ("LineColor", Color) = (1,1,0,1)
        _LineWidth ("LIneWidth", Range(0.01, 0.3)) = 0.1
        _BlendIntensity ("BlendIndensity", Range(0,1)) = 1
		_PurifiedTex("AfterTex",2D) = "black"{}
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LineWidth;
            float _BlendIntensity;
			sampler2D _PurifiedTex;
			float4 _PurifiedTex_ST;
			float4 _LineColor;

            // 全局膨胀球参数
            float3 _PurifySphereCenter;
            float _PurifySphereRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // 世界坐标
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 原始模型颜色
                fixed4 col = tex2D(_MainTex, i.uv);

                // 求交：点到球心距离
                float dist = length(i.worldPos - _PurifySphereCenter);

                // 1. 是否在球内
                float inner = step(dist, _PurifySphereRadius);

                // 2. 是否在相交描边
                float lineCross = step(dist, _PurifySphereRadius + _LineWidth)
                           - step(dist, _PurifySphereRadius);

				fixed4 AfterCol = tex2D(_PurifiedTex,i.uv);
                fixed4 finalCol = col;
                finalCol = lerp(finalCol, AfterCol, inner * _BlendIntensity);
                finalCol = lerp(finalCol, _LineColor, lineCross);


                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "Standard"
}