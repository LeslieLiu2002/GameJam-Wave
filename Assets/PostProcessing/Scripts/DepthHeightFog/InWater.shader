Shader "Hidden/SimpleDepthFog"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        
        [Header(General)]
        _WaterLevel ("Water Level (Y)", Float) = 0.0
        _StartDistance ("Start Distance", Float) = 0.0
        
        [Header(Colors)]
        _WaterShallowColor ("Shallow Color", Color) = (0.3, 0.8, 0.9, 1)
        _WaterDeepColor ("Deep Color", Color) = (0.0, 0.1, 0.3, 1)
        _UnderwaterFogBrightness ("Fog Brightness", Float) = 1.0
        _HeightFogBrightness ("Height Fog Brightness", Float) = 1.0

        [Header(Density)]
        _UnderwaterFogDensity ("Distance Fog Density", Float) = 0.05
        _HeightFogDensity ("Height Fog Density", Float) = 0.5
        _HeightFogDepth ("Height Fog Offset", Float) = 0.0

        [Header(Distortion)]
        _NoiseTexture ("Noise Texture", 2D) = "black" {}
        _DistortionSpeed ("Distortion Speed (XY Layer1, ZW Layer2)", Vector) = (0.05, 0.05, -0.05, -0.05)
        _DistortionStrength ("Distortion Strength", Float) = 0.02
        _EdgeFade ("Screen Edge Fade", Float) = 0.1

        [Header(Helmet Mask)]
        [Toggle(_HELMET_MASK_ON)] _HelmetMaskEnabled ("Enable Helmet Mask", Float) = 0
        _HelmetColor ("Helmet Color", Color) = (0, 0, 0, 1)
        _LensDistortionStrength ("Lens Distortion Strength", Float) = 0.0
        _MaskHardness ("Mask Hardness", Float) = 400.0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            Name "Stylized Underwater Fog with Distortion"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _HELMET_MASK_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float _WaterLevel;
                float _StartDistance;
                
                half4 _WaterShallowColor;
                half4 _WaterDeepColor;
                float _UnderwaterFogBrightness;
                float _HeightFogBrightness;

                float _UnderwaterFogDensity;
                float _HeightFogDensity;
                float _HeightFogDepth;

                float4 _DistortionSpeed;
                float _DistortionStrength;
                float _EdgeFade;

                // Helmet Params
                half4 _HelmetColor;
                float _LensDistortionStrength;
                float _MaskHardness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // ------------------------------------------------------------------
            // 功能函数封装
            // ------------------------------------------------------------------

            // 1. 屏幕边缘遮罩 (用于水下扭曲的边缘剔除)
            float GetScreenEdgeMask(float2 uv, float edgeScale)
            {
                float2 s1 = saturate(uv * edgeScale);
                float2 s2 = saturate((1.0 - uv) * edgeScale);
                float2 mask = s1 * s2;
                return mask.x * mask.y;
            }

            // 2. 水下扰动计算
            float2 GetDistortion(float2 uv, float time)
            {
                float2 uv1 = uv + _DistortionSpeed.xy * time;
                float2 uv2 = uv + _DistortionSpeed.zw * time;
                half4 noise1 = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uv1);
                half4 noise2 = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uv2);
                float2 distort = (noise1.rg + noise2.rg) - 1.0;
                return distort;
            }

            // 3. 透镜畸变 (Lens Warp) - 模拟面罩折射
            // 基于 (UV - 0.5) * DistanceSq 逻辑
            float2 GetLensWarp(float2 uv, float strength)
            {
                float2 center = float2(0.5, 0.5);
                float2 offset = uv - center;
                // 计算距离平方 (Offset.x^2 + Offset.y^2)
                float r2 = dot(offset, offset);
                // 偏移量 = 原始偏移 + 额外基于距离的偏移
                // 简单的桶形/枕形畸变模型
                return uv + offset * (r2 * strength);
            }

            // 4. 面罩遮罩 (Round Square Mask)
            // 逻辑: Saturate( (uv.x * (1-uv.x) * uv.y * (1-uv.y)) * Hardness )
            float GetRoundSquareMask(float2 uv, float hardness)
            {
                float2 p = uv * (1.0 - uv); // x*(1-x), y*(1-y)
                float maskVal = p.x * p.y;  // 此时中心最大值为 0.25*0.25 = 0.0625
                return saturate(maskVal * hardness); 
            }

            // ------------------------------------------------------------------
            // 雾计算逻辑
            // ------------------------------------------------------------------
            float ComputeDistanceXYZ(float3 positionWS)
            {
                float horizontal = length(_WorldSpaceCameraPos.xz - positionWS.xz);
                horizontal -= _ProjectionParams.y + _StartDistance;
                horizontal *= _UnderwaterFogDensity;
                return saturate(1.0 - (exp(-horizontal)));
            }

            float ComputeUnderwaterFogHeight(float3 positionWS)
            {
                float start = (_WaterLevel - 1.0 - _HeightFogDepth) - _HeightFogDensity;
                float3 wsDir = _WorldSpaceCameraPos.xyz - positionWS;
                float FH = start; 
                float3 P = positionWS;
                float FdotC = _WorldSpaceCameraPos.y - start; 
                float k = (FdotC <= 0.0f ? 1.0f : 0.0f); 
                float FdotP = P.y - FH;
                float FdotV = wsDir.y;
                float c1 = k * (FdotP + FdotC);
                float c2 = (1.0 - 2.0 * k) * FdotP;
                float g = min(c2, 0.0);
                g = -_HeightFogDensity * (c1 - g * g / abs(FdotV + 1.0e-5f));
                return 1.0 - exp(-g);
            }

            float3 GetUnderwaterFogColor(float distanceDensity, float heightDensity)
            {
                float3 waterColor = lerp(_WaterShallowColor.rgb, _WaterDeepColor.rgb, distanceDensity) * _UnderwaterFogBrightness;
                waterColor = lerp(waterColor, _WaterDeepColor.rgb * _HeightFogBrightness, heightDensity);
                return waterColor;
            }

            // ------------------------------------------------------------------
            // Fragment Shader
            // ------------------------------------------------------------------

            half4 frag(Varyings i) : SV_Target
            {
                float2 finalUV = i.uv;

                // [Helmet Step 1] 如果开启面罩，先应用透镜畸变
                #if _HELMET_MASK_ON
                    finalUV = GetLensWarp(finalUV, _LensDistortionStrength);
                #endif

                // [Fog Step 1] 计算屏幕边缘淡出 (基于当前的 UV，如果是畸变后的UV，边缘也会跟着畸变，更自然)
                float edgeScale = 1.0 / max(_EdgeFade, 0.001);
                float screenMask = GetScreenEdgeMask(finalUV, edgeScale);

                // [Fog Step 2] 计算水下扰动
                float2 distortion = GetDistortion(finalUV, _Time.y);
                distortion *= _DistortionStrength * screenMask;
                
                // 应用水下扰动到 UV
                float2 fogUV = finalUV + distortion;

                // [Sample] 采样颜色和深度
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, fogUV);
                float rawDepth = SampleSceneDepth(fogUV);
                float3 worldPos = ComputeWorldSpacePosition(fogUV, rawDepth, UNITY_MATRIX_I_VP);

                // [Fog Step 3] 计算雾
                float distDensity = ComputeDistanceXYZ(worldPos);
                float heightDensity = ComputeUnderwaterFogHeight(worldPos);
                float totalFogFactor = saturate(distDensity + heightDensity);

                #if UNITY_REVERSED_Z
                    if(rawDepth < 0.0001) totalFogFactor = 0;
                #else
                    if(rawDepth > 0.9999) totalFogFactor = 0;
                #endif

                float3 fogColorRGB = GetUnderwaterFogColor(distDensity, heightDensity);
                half3 finalSceneColor = lerp(originalColor.rgb, fogColorRGB, totalFogFactor);

                // [Helmet Step 2] 如果开启面罩，应用面罩遮挡混合
                #if _HELMET_MASK_ON
                    // 计算面罩 Alpha (白色为可视区域，黑色为面罩遮挡)
                    // 这里传入 finalUV (应用了透镜畸变但未应用水波扰动的UV，保证面罩形状稳定)
                    // 或者传入 fogUV (让面罩边缘也跟着水波晃动)。通常面罩是贴在脸上的，不应该随水波晃动。
                    // 所以我们使用 finalUV (LensWarped UV)
                    float helmetMask = GetRoundSquareMask(finalUV, _MaskHardness);
                    
                    // 混合: mask为1显示场景，mask为0显示面罩颜色
                    finalSceneColor = lerp(_HelmetColor.rgb, finalSceneColor, helmetMask);
                #endif
                
                return half4(finalSceneColor,originalColor.a);
            }
            ENDHLSL
        }
    }
}