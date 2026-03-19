// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable

Shader "Hidden/PostProcessPurify"
{
    Properties
    {
        _PurifiedTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off ZTest Always

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
                float4 viewRay : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _LineColor;
            float _LineWidth;
            float _BlendIntensity;
            sampler2D _PurifiedTex;
            float4 _PurifiedTex_ST;

            // 膨胀球
            float3 _PurifySphereCenter;
            float _PurifySphereRadius;
            // float4x4 _CameraToWorld;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.viewRay = mul(unity_CameraInvProjection, float4(v.uv*2-1, 0, 1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 原画面颜色
                fixed4 col = tex2D(_MainTex, i.uv);

                // 计算像素对应的世界坐标
                float3 viewRay = i.viewRay.xyz / i.viewRay.w;
                float3 worldPos = _WorldSpaceCameraPos + viewRay * _ProjectionParams.z;

                // 球相交计算
                float dist = length(worldPos - _PurifySphereCenter);
                float inner = step(dist, _PurifySphereRadius);
                float lineCross = step(dist, _PurifySphereRadius + _LineWidth) - step(dist, _PurifySphereRadius);

                // 效果叠加：不破坏原图
                fixed4 afterCol = tex2D(_PurifiedTex, i.uv);
                fixed4 final = col;
                final = lerp(final, afterCol, inner * _BlendIntensity);
                final = lerp(final, _LineColor, lineCross);

                return final;
            }
            ENDCG
        }
    }
}