Shader "Custom/TryAnim"
{
    Properties
    {
        _HoloColor ("Hologram Color", Color) = (0.2, 0.8, 1.0, 0.3)
        _EdgeColor ("Edge Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _EdgeWidth ("Edge Width", Range(0, 0.05)) = 0.01
        _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.7
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.5
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

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
            };

            float4 _HoloColor;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _EdgeThreshold;
            float _GlowIntensity;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 1. 基础半透明颜色
                half4 finalColor = _HoloColor;

                // 2. 视线方向
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normal = normalize(i.normalWS);

                // 3. 边缘检测（描边核心）
                float NdotV = saturate(dot(normal, viewDir));
                float edge = 1 - NdotV;
                edge = smoothstep(_EdgeThreshold, 1.0, edge);

                // 4. 叠加发光描边
                float3 edgeColor = _EdgeColor.rgb * edge * _GlowIntensity;
                finalColor.rgb = lerp(finalColor.rgb, edgeColor, edge);
                finalColor.a = _HoloColor.a + edge * 0.5;

                return finalColor;
            }
            ENDHLSL
        }

        // 外描边 Pass（让轮廓更粗更亮）
        Pass
        {
            Cull Front
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

            float4 _EdgeColor;
            float _EdgeWidth;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 pos = v.positionOS.xyz + normalize(v.normalOS) * _EdgeWidth;
                o.positionHCS = TransformObjectToHClip(pos);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return _EdgeColor;
            }
            ENDHLSL
        }
    }
}