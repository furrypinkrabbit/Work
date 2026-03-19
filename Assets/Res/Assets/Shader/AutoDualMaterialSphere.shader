Shader "Custom/AutoDualMaterialSphere"
{
    Properties
    {
        _LineColor ("LineColor", Color) = (1,1,0,1)
        _LineWidth ("LineWidth", Range(0.01, 0.3)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ORIGINAL"
            ZWrite On
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
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float3 _PurifySphereCenter;
            float _PurifySphereRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = length(i.worldPos - _PurifySphereCenter);
                if (d < _PurifySphereRadius) discard;
                return fixed4(1,1,1,1); // 由C#自动替换原材质
            }
            ENDCG
        }


        Pass
        {
            Name "NEW_MAT"
            ZWrite On
            Blend Off
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
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float3 _PurifySphereCenter;
            float _PurifySphereRadius;
            float4 _LineColor;
            float _LineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = length(i.worldPos - _PurifySphereCenter);
                if (d > _PurifySphereRadius + _LineWidth + 0.01) discard;

                float lineC = step(d, _PurifySphereRadius + _LineWidth) - step(d, _PurifySphereRadius);
                fixed4 col = fixed4(1,1,1,1); // 由C#自动替换新材质
                col = lerp(col, _LineColor, lineC);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}