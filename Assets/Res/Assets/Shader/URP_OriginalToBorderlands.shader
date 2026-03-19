Shader "Custom/URP_OriginalToBorderlands"
{
    Properties
    {
        //========== 原材质属性（自动复制）==========
        _MainTex ("原材质主纹理", 2D) = "white" {}
        _BaseColor ("原材质颜色", Color) = (1,1,1,1)
        
        //========== BorderlandsGun 属性 ==========
        _BL_BaseColor ("BL 基础色", Color) = (0.8,0.8,0.8,1)
        _BL_ShadowColor ("BL 阴影色", Color) = (0.3,0.3,0.35,1)
        _BL_OutlineColor ("BL 轮廓色", Color) = (0,0,0,1)
        _BL_OutlineWidth ("BL 轮廓宽度", Range(0, 0.05)) = 0.01
        _BL_LightThreshold ("BL 光照阈值", Range(0,1)) = 0.5
        _BL_Gloss ("BL 光泽度", Range(0,1)) = 0.2
        
        //========== 球体相交描边 ==========
        _SphereLineColor ("相交线颜色", Color) = (1,1,0,1)
        _SphereLineWidth ("相交线宽度", Range(0.01, 0.3)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        //====================================================================
        // Pass 1: 主渲染（球外=原材质，球内=BorderlandsGun）
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

            //========== 材质属性 ==========
            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            
            float4 _BL_BaseColor;
            float4 _BL_ShadowColor;
            float4 _BL_OutlineColor;
            float _BL_OutlineWidth;
            float _BL_LightThreshold;
            float _BL_Gloss;
            
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
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            //========== BorderlandsGun 完整逻辑 ==========
            half3 BorderlandsGunLogic(float3 normalWS, float3 positionWS, float2 uv)
            {
                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDirWS));

                float lightLevel = step(_BL_LightThreshold, NdotL);
                float3 finalColor = lerp(_BL_ShadowColor.rgb, _BL_BaseColor.rgb, lightLevel);

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(positionWS));
                float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDirWS)), _BL_Gloss * 100);
                finalColor += spec * 0.3;

                float edge = 1 - saturate(dot(normalWS, viewDirWS));
                edge = step(0.8, edge);
                finalColor = lerp(finalColor, _BL_OutlineColor.rgb, edge * 0.5);

                return finalColor;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 计算到球的距离
                float dist = length(input.positionWS - _PurifySphereCenter);
                float isInSphere = step(dist, _PurifySphereRadius);
                float isInLine = step(dist, _PurifySphereRadius + _SphereLineWidth) - step(dist, _PurifySphereRadius);

                // 2. 球外：原材质（主纹理+颜色+简单光照）
                half3 finalColor;
                if (isInSphere < 0.5)
                {
                    Light mainLight = GetMainLight();
                    float3 normalWS = normalize(input.normalWS);
                    float NdotL = saturate(dot(normalWS, normalize(mainLight.direction)));
                    half4 texColor = tex2D(_MainTex, input.uv) * _BaseColor;
                    finalColor = texColor.rgb * (mainLight.color * NdotL + 0.2); // 简单漫反射+环境光
                }
                // 3. 球内：BorderlandsGun
                else
                {
                    finalColor = BorderlandsGunLogic(normalize(input.normalWS), input.positionWS, input.uv);
                }

                // 4. 相交线高亮
                finalColor = lerp(finalColor, _SphereLineColor.rgb, isInLine);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        //====================================================================
        // Pass 2: BorderlandsGun 轮廓（仅球内显示）
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
            float _BL_OutlineWidth;
            float4 _BL_OutlineColor;
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
                
                float3 offset = normalize(normalViewWS) * _BL_OutlineWidth;
                float3 finalPosWS = positionWS + offset;
                output.positionHCS = TransformWorldToHClip(finalPosWS);
                output.positionWS = positionWS;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 仅球内显示轮廓
                float dist = length(input.positionWS - _PurifySphereCenter);
                if (dist > _PurifySphereRadius) discard;
                
                return _BL_OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}