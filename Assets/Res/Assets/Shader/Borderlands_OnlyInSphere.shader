Shader "Custom/Borderlands_OnlyInSphere"
{
    Properties
    {
        // Borderlands 核心属性
        _BaseColor ("Base Color", Color) = (0.8,0.8,0.8,1)
        _ShadowColor ("Shadow Color", Color) = (0.3,0.3,0.35,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.01
        _LightThreshold ("Light Threshold", Range(0,1)) = 0.5
        _Gloss ("Gloss", Range(0,1)) = 0.2
        
        // 球体相交描边
        _SphereLineColor ("相交线颜色", Color) = (1,1,0,1)
        _SphereLineWidth ("相交线宽度", Range(0.01, 0.3)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" } // 比原材质晚渲染
        LOD 100
        ZWrite On
        ZTest LEqual

        //====================================================================
        // Pass 1: 球内卡通渲染 + 相交描边
        //====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _ShadowColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _LightThreshold;
            float _Gloss;
            
            // 全局膨胀球参数
            float3 _PurifySphereCenter;
            float _PurifySphereRadius;
            float4 _SphereLineColor;
            float _SphereLineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 【核心】球外像素直接丢弃，完全不影响原材质
                float dist = length(input.positionWS - _PurifySphereCenter);
                if (dist > _PurifySphereRadius + _SphereLineWidth) discard;

                // 1. 计算相交描边区域
                float isInLine = step(dist, _PurifySphereRadius + _SphereLineWidth) - step(dist, _PurifySphereRadius);

                // 2. 完整的Borderlands卡通渲染逻辑
                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, lightDirWS));

                float lightLevel = step(_LightThreshold, NdotL);
                float3 finalColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, lightLevel);

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDirWS)), _Gloss * 100);
                finalColor += spec * 0.3;

                float edge = 1 - saturate(dot(normalWS, viewDirWS));
                edge = step(0.8, edge);
                finalColor = lerp(finalColor, _OutlineColor.rgb, edge * 0.5);

                // 3. 叠加相交描边（优先级最高）
                finalColor = lerp(finalColor, _SphereLineColor.rgb, isInLine);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        //====================================================================
        // Pass 2: 卡通轮廓线（仅球内显示）
        //====================================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

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
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float _OutlineWidth;
            float4 _OutlineColor;
            float3 _PurifySphereCenter;
            float _PurifySphereRadius;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 viewDirWS = GetWorldSpaceViewDir(positionWS);
                float3 normalViewWS = normalize(normalWS + viewDirWS * 0.1);
                
                float3 offset = normalize(normalViewWS) * _OutlineWidth;
                float3 finalPosWS = positionWS + offset;
                output.positionHCS = TransformWorldToHClip(finalPosWS);
                output.positionWS = positionWS;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 仅球内显示轮廓，球外直接丢弃
                float dist = length(input.positionWS - _PurifySphereCenter);
                if (dist > _PurifySphereRadius) discard;
                
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}