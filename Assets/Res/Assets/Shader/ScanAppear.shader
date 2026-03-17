Shader "Custom/ScanAppear_KeepOriginalColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanColor ("Scan Line Color", Color) = (0,1,1,1)
        _ScanWidth ("Scan Width", Range(0.001, 0.1)) = 0.02
        _ScanSpeed ("Scan Speed", Float) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 pos : SV_POSITION;
                float y : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ScanColor;
            float _ScanWidth;
            float _ScanSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.y = v.vertex.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 扫描位置
                float pos = fmod(_Time.y * _ScanSpeed, 1.1);
                float h = saturate(i.y + 0.5);
                
                // 透明度：低于扫描线 → 显示
                fixed a = step(h, pos);

                // 扫描线高光（只影响线条，不影响模型颜色）
                float f = smoothstep(pos - _ScanWidth, pos, h);
                f -= smoothstep(pos, pos + _ScanWidth, h);

                // 最终颜色：完全保留原图 + 加一点点扫描线高光
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb = c.rgb + _ScanColor.rgb * f * 0.5;
                c.a = a;

                return c;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}