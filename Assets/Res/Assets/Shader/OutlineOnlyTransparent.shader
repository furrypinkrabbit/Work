Shader "Custom/OutlineOnlyTransparent"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // --------------------
        // 唯一 Pass：纯描边
        // --------------------
        Pass
        {
            Cull Front // 只渲染背面，实现外描边

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            Varyings vert (Attributes v)
            {
                Varyings o;

                // 核心：沿着法线方向扩大模型 = 描边
                float3 pos = v.positionOS.xyz;
                float3 normal = normalize(v.normalOS);
                pos += normal * _OutlineWidth;

                o.positionHCS = TransformObjectToHClip(pos);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 只输出描边颜色，模型完全透明
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}