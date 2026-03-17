Shader "Custom/BorderlandsGun_Final"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8,0.8,0.8,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.01
        _Gloss ("Gloss", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

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
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Gloss;
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
                // 正常真实光照
                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, lightDirWS));

                // 正常渐变光照（删除卡通阶）
                float3 finalColor = _BaseColor.rgb * NdotL;

                // 保留高光
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDirWS)), _Gloss * 100);
                finalColor += spec * 0.3;

                // 保留边缘线条
                float edge = 1 - saturate(dot(normalWS, viewDirWS));
                edge = step(0.8, edge);
                finalColor = lerp(finalColor, _OutlineColor.rgb, edge * 0.5);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // 描边 Pass（干净、不黑坨）
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
            };

            CBUFFER_START(UnityPerMaterial)
            float _OutlineWidth;
            float4 _OutlineColor;
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
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}