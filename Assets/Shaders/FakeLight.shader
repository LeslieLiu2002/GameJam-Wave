Shader "Wave/FakeLightSphere_MPB"
{
    Properties
    {
        [Header(Core Settings)]
        [HDR]_ScanColor0("Scan Color 0", Color) = (1,1,1,1)
        [HDR]_ScanColor1("Scan Color 1", Color) = (0,0,1,1)
        [HDR]_ScanColor2("Scan Color 2", Color) = (0,0,1,1)
        
        // [新增] MPB 控制变量
        _OverrideRadius("Radius (Controlled by Script)", Float) = 1.0
        _EmissionStrength("Emission Strength", Float) = 1.0

        _GradientTexture("Gradient Texture (Distance -> Color)", 2D) = "white" {}
        _NoiseTexture("Noise Texture", 2D) = "white" {}
        
        [Header(Blending)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1 
        [Enum(UnityEngine.Rendering.BlendMode)] 
        _DstBlend("Dest Blend", Float) = 1 
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0
        
        [Header(Softness)]
        _Softness("Intersection Softness", Range(0.01, 2.0)) = 1.0
        
        [Header(Animation)]
        _DetectSpeed("Radius Detect Speed", Range(0.01, 1.0)) = 0.1
        _NoiseAnimSpeed("Noise Animation Speed", Float) = 0.2

        [Header(Distance Tiling)]
        // [新增] 用于控制距离越远，纹理越大的属性
        _BaseTiling("Base Tiling (Near)", Float) = 0.1  // 以前写死的 0.1
        _FarTiling("Far Tiling (Far)", Float) = 0.02    // 远处建议设小，比如 0.02
        _TilingDist("Tiling Transition Distance", Float) = 50.0 // 多少米开始完全变成大纹理

        [Toggle(_FLICKER_ON)] 
        _FlickerOn("Enable Flicker", Float) = 0
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

            ZTest Always
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _FLICKER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // 全局变量
            TEXTURE2D(_Global_WaterNoiseTexture);
            SAMPLER(sampler_Global_WaterNoiseTexture);
            float4 _Global_WaterDistortionSpeed;
            float _Global_WaterDistortionStrength;
            float _Global_WaterEdgeFade;
            float _Global_HelmetMaskEnabled;
            float _Global_LensDistortionStrength;

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LightTint;
                float _Softness;
                float4 _GradientTexture_ST,_NoiseTexture_ST;
                float _FlickerSpeed;
                float _FlickerIntensity;
                float _DetectSpeed;
                float _NoiseAnimSpeed;
                float _BaseTiling;
                float _FarTiling;
                float _TilingDist;

                float4 _ScanColor0, _ScanColor1, _ScanColor2;
                
                // [新增] CBuffer 变量
                float _OverrideRadius;
                float _EmissionStrength;
            CBUFFER_END

            TEXTURE2D(_GradientTexture);
            SAMPLER(sampler_GradientTexture);
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                
                #if _FLICKER_ON
                    float noise = sin(_Time.y * _FlickerSpeed * 10.0) * 0.5 + 0.5;
                    float flicker = lerp(1.0, noise, _FlickerIntensity);
                    output.color.rgb *= flicker;
                #endif

                return output;
            }

            // --- 移植函数保持不变 ---
            float GetWaterScreenEdgeMask(float2 uv, float edgeScale) {
                float2 s1 = saturate(uv * edgeScale);
                float2 s2 = saturate((1.0 - uv) * edgeScale);
                float2 mask = s1 * s2; return mask.x * mask.y;
            }
            float2 GetWaterDistortion(float2 uv, float time) {
                float2 uv1 = uv + _Global_WaterDistortionSpeed.xy * time;
                float2 uv2 = uv + _Global_WaterDistortionSpeed.zw * time;
                half4 noise1 = SAMPLE_TEXTURE2D(_Global_WaterNoiseTexture, sampler_Global_WaterNoiseTexture, uv1);
                half4 noise2 = SAMPLE_TEXTURE2D(_Global_WaterNoiseTexture, sampler_Global_WaterNoiseTexture, uv2);
                float2 distort = (noise1.rg + noise2.rg) - 1.0; return distort;
            }
            float2 GetWaterLensWarp(float2 uv, float strength) {
                float2 center = float2(0.5, 0.5);
                float2 offset = uv - center; float r2 = dot(offset, offset); return uv + offset * (r2 * strength);
            }
            float2 GetDistortedWaterDepthUV(float2 screenUV) {
                float2 finalUV = screenUV;
                if (_Global_HelmetMaskEnabled > 0.5) finalUV = GetWaterLensWarp(finalUV, _Global_LensDistortionStrength);
                float edgeScale = 1.0 / max(_Global_WaterEdgeFade, 0.001);
                float screenMask = GetWaterScreenEdgeMask(finalUV, edgeScale);
                float2 distortion = GetWaterDistortion(finalUV, _Time.y);
                distortion *= _Global_WaterDistortionStrength * screenMask; return finalUV + distortion;
            }
            // ------------------------

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 rawScreenUV = input.screenPos.xy / input.screenPos.w;
                float2 distortedUV = GetDistortedWaterDepthUV(rawScreenUV);

                float rawDepth = SampleSceneDepth(distortedUV);
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 sceneWorldPos = ComputeWorldSpacePosition(distortedUV, rawDepth, UNITY_MATRIX_I_VP);
                
                float3 lightCenter = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                
                // --- 世界空间三向混合 (Triplanar Mapping) ---
                float3 worldNormal = normalize(cross(ddy(sceneWorldPos), ddx(sceneWorldPos)));
                float3 blendWeights = abs(worldNormal);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z + 1e-6);
                
                // 计算像素到摄像机的距离
                float distToCamera = distance(sceneWorldPos, _WorldSpaceCameraPos);
                
                // 距离越远，scale 越接近 _FarTiling (0.02)，纹理越大，重复越少
                float currentTilingScale = lerp(_BaseTiling, _FarTiling, saturate(distToCamera / _TilingDist));

                // 动画偏移
                float2 noiseScroll = _Time.y * _NoiseAnimSpeed;
                
                float2 uvX = sceneWorldPos.zy * currentTilingScale + noiseScroll;
                float2 uvY = sceneWorldPos.xz * currentTilingScale + noiseScroll;
                float2 uvZ = sceneWorldPos.xy * currentTilingScale + noiseScroll;
                
                float noiseX = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, TRANSFORM_TEX(uvX, _NoiseTexture)).r;
                float noiseY = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, TRANSFORM_TEX(uvY, _NoiseTexture)).r;
                float noiseZ = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, TRANSFORM_TEX(uvZ, _NoiseTexture)).r;
                
                float noiseMask = noiseX * blendWeights.x + noiseY * blendWeights.y + noiseZ * blendWeights.z;

                // [修改] 替换原有的 Scale 获取逻辑，改为使用 MPB 传入的 _OverrideRadius
                // float3 scale;
                // scale.x = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
                // ...
                // float lightRadius = max(scale.x, max(scale.y, scale.z)) * 0.5;
                
                float lightRadius = _OverrideRadius;
                lightRadius = max(0.001, lightRadius);

                float distToLight = distance(sceneWorldPos, lightCenter);
                
                float distanceFalloff = saturate(distToLight / lightRadius);
                
                float2 gradientUV = float2(distanceFalloff - _Time.y*_DetectSpeed, 0.5);
                gradientUV = TRANSFORM_TEX(gradientUV,_GradientTexture);
                noiseMask *= SAMPLE_TEXTURE2D(_GradientTexture, sampler_GradientTexture, gradientUV).r;

                float3 gradientColor = lerp(_ScanColor0, _ScanColor1, smoothstep(0, 0.1,noiseMask));
                gradientColor = lerp(gradientColor, _ScanColor2, smoothstep(0.1,0.16, noiseMask));
                
                half3 finalColor = gradientColor;
                finalColor *= _EmissionStrength;

                float finalAlpha = (1-distanceFalloff)*noiseMask.r;

                #if UNITY_REVERSED_Z
                    if (rawDepth == 0) return 0;
                #else
                    if (rawDepth == 1) return 0;
                #endif

                return float4(finalColor,finalAlpha);
            }
            ENDHLSL
        }
    }
}