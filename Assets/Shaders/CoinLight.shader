Shader "LazyEti/URP/FakePointLight_Simplified"
{
    Properties
    {
        [Header(Core Settings)]
        [MainColor] _LightTint("Light Tint", Color) = (1,1,1,1)
        [NoScaleOffset][SingleLineTexture]_GradientTexture("Gradient Texture (Distance -> Color)", 2D) = "white" {}
        
        [Header(Halo Settings)]
        [Toggle(_HALO_ON)] _HaloOn("Enable Halo Layer", Float) = 1
        [HDR] _HaloColor("Halo Color", Color) = (1,1,1,1)
        _HaloSize("Halo Size (Relative)", Range(0.0, 1.0)) = 0.2
        _HaloHardness("Halo Hardness", Range(1.0, 50.0)) = 10.0
        _HaloDepthFade("Halo Depth Fade", Range(0.0, 2.0)) = 0.5

        [Header(Blending)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1 // One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dest Blend", Float) = 1 // One (Additive)
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0
        
        [Header(Softness)]
        _Softness("Intersection Softness", Range(0.01, 2.0)) = 1.0
        
        [Header(Animation)]
        [Toggle(_FLICKER_ON)] _FlickerOn("Enable Flicker", Float) = 0
        _FlickerSpeed("Flicker Speed", Range(0, 10)) = 1
        _FlickerIntensity("Flicker Intensity", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline" 
            "RenderType"="Overlay" 
            "Queue"="Transparent+100" 
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            // 核心配置：总是通过深度测试
            ZTest Always
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // 变体
            #pragma shader_feature_local _FLICKER_ON
            #pragma shader_feature_local _HALO_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LightTint;
                float _Softness;
                float _FlickerSpeed;
                float _FlickerIntensity;
                
                // Halo 属性
                half4 _HaloColor;
                float _HaloSize;
                float _HaloHardness;
                float _HaloDepthFade;
            CBUFFER_END

            TEXTURE2D(_GradientTexture);
            SAMPLER(sampler_GradientTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.color = input.color * _LightTint;

                #if _FLICKER_ON
                    float noise = sin(_Time.y * _FlickerSpeed * 10.0) * 0.5 + 0.5;
                    float flicker = lerp(1.0, noise, _FlickerIntensity);
                    output.color.rgb *= flicker;
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 1. 准备屏幕 UV 和深度
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // 2. 重建世界坐标
                float3 sceneWorldPos = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);

                // 3. 处理天空盒剔除
                #if UNITY_REVERSED_Z
                    if (rawDepth == 0) return 0;
                #else
                    if (rawDepth == 1) return 0;
                #endif

                // 4. 获取光源中心和半径
                float3 lightCenter = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float3 scale = float3(
                    length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                    length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)),
                    length(float3(UNITY_MATRIX_M._m02, UNITY_MATRIX_M._m12, UNITY_MATRIX_M._m22))
                );
                float lightRadius = max(scale.x, max(scale.y, scale.z)) * 0.5;

                // 5. 计算距离
                float distToLight = distance(sceneWorldPos, lightCenter);

                // 6. 软粒子/深度相交计算 (用于 Base 和 Halo)
                float fragmentLinearDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDifference = sceneLinearDepth - fragmentLinearDepth;
                // 防止 depthDifference 小于0 (物体在光球前方)
                float intersection = saturate(depthDifference / _Softness);

                // --- Layer 1: Base Gradient Light (原有逻辑) ---
                float baseDistFalloff = saturate(distToLight / lightRadius);
                half4 baseColor = SAMPLE_TEXTURE2D(_GradientTexture, sampler_GradientTexture, float2(baseDistFalloff, 0));
                // 应用 Base 颜色和相交淡出
                baseColor *= input.color * intersection;

                // --- Layer 2: Halo Light (新增逻辑) ---
                half4 finalHalo = 0;
                
                #if _HALO_ON
                    // Halo 的半径是基于整体半径的一个比例
                    float haloRadius = lightRadius * _HaloSize;
                    
                    // 计算 Halo 的距离衰减 (0 = 中心, 1 = 边缘)
                    // 使用 _HaloHardness 指数来控制边缘有多硬
                    float haloFalloff = saturate(1.0 - (distToLight / haloRadius));
                    haloFalloff = pow(haloFalloff, _HaloHardness);
                    
                    // Halo 的独立深度淡出 (通常希望光晕核心不要穿插得太生硬)
                    float haloIntersection = saturate(depthDifference / _HaloDepthFade);
                    
                    finalHalo = _HaloColor * haloFalloff * haloIntersection;
                    
                    // 应用 Flicker 到 Halo
                    #if _FLICKER_ON
                        // 简单的复用 vertex color 里的 flicker 信息，或者重新计算
                        // 这里我们假设 input.color 已经包含了 flicker 变暗
                        // 我们提取 flicker 强度比例 (rgb均值 / _LightTint均值) 稍微麻烦
                        // 简单做法：直接乘 input.color 的亮度增益
                        finalHalo *= (input.color.r + input.color.g + input.color.b) / 3.0; 
                    #endif
                #endif

                // 7. 最终合成
                // 使用加法混合 (Additive) 叠加 Halo
                half4 finalColor = baseColor + finalHalo;

                return finalColor;
            }
            ENDHLSL
        }
    }
}