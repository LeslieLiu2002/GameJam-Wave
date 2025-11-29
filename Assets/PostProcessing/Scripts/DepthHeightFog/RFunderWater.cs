using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleRendererFeature("Custom Exponential Height Fog")]
public sealed class RFunderWater : ScriptableRendererFeature
{
    [Tooltip("用于指数高度雾的着色器")]
    public Shader fogShader;

    private Material m_Material;
    private ExponentialHeightFogPass m_RenderPass;

    public override void Create()
    {
        if (fogShader == null)
        {
            Debug.LogWarning("ExponentialHeightFogFeature: 未分配 Fog Shader，将禁用此功能。");
            return;
        }

        m_Material = CoreUtils.CreateEngineMaterial(fogShader);
        m_RenderPass = new ExponentialHeightFogPass(m_Material);
        
        m_RenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null)
            return;

        var stack = VolumeManager.instance.stack;
        var fogComponent = stack.GetComponent<ExponentialHeightFog>();

        if (fogComponent == null || !fogComponent.IsActive())
            return;
            
        if (!renderingData.cameraData.camera.TryGetCullingParameters(out var cullingParameters))
            return;
            
        m_RenderPass.Setup(fogComponent);
        renderer.EnqueuePass(m_RenderPass);
    }
    
    internal sealed class ExponentialHeightFogPass : ScriptableRenderPass
    {
        private Material m_Material;
        private ExponentialHeightFog m_Component;
        
        private readonly int m_TempRT_ID = Shader.PropertyToID("_HeightFogTempRT");

        // Shader Property IDs (Local Material)
        private static readonly int _WaterLevel = Shader.PropertyToID("_WaterLevel");
        private static readonly int _StartDistance = Shader.PropertyToID("_StartDistance");
        private static readonly int _WaterShallowColor = Shader.PropertyToID("_WaterShallowColor");
        private static readonly int _WaterDeepColor = Shader.PropertyToID("_WaterDeepColor");
        private static readonly int _UnderwaterFogBrightness = Shader.PropertyToID("_UnderwaterFogBrightness");
        private static readonly int _HeightFogBrightness = Shader.PropertyToID("_HeightFogBrightness");
        private static readonly int _UnderwaterFogDensity = Shader.PropertyToID("_UnderwaterFogDensity");
        private static readonly int _HeightFogDensity = Shader.PropertyToID("_HeightFogDensity");
        private static readonly int _HeightFogDepth = Shader.PropertyToID("_HeightFogDepth");

        // Distortion IDs (Local Material)
        private static readonly int _NoiseTexture = Shader.PropertyToID("_NoiseTexture");
        private static readonly int _DistortionSpeed = Shader.PropertyToID("_DistortionSpeed");
        private static readonly int _DistortionStrength = Shader.PropertyToID("_DistortionStrength");
        private static readonly int _EdgeFade = Shader.PropertyToID("_EdgeFade");

        // Helmet Mask IDs (Local Material)
        private static readonly int _HelmetColor = Shader.PropertyToID("_HelmetColor");
        private static readonly int _LensDistortionStrength = Shader.PropertyToID("_LensDistortionStrength");
        private static readonly int _MaskHardness = Shader.PropertyToID("_MaskHardness");

        // --- Global Variable IDs (For syncing with FakeLight etc.) ---
        private static readonly int _Global_WaterNoiseTexture = Shader.PropertyToID("_Global_WaterNoiseTexture");
        private static readonly int _Global_WaterDistortionSpeed = Shader.PropertyToID("_Global_WaterDistortionSpeed");
        private static readonly int _Global_WaterDistortionStrength = Shader.PropertyToID("_Global_WaterDistortionStrength");
        private static readonly int _Global_WaterEdgeFade = Shader.PropertyToID("_Global_WaterEdgeFade");
        private static readonly int _Global_HelmetMaskEnabled = Shader.PropertyToID("_Global_HelmetMaskEnabled");
        private static readonly int _Global_LensDistortionStrength = Shader.PropertyToID("_Global_LensDistortionStrength");

        public ExponentialHeightFogPass(Material material)
        {
            m_Material = material;
            profilingSampler = new ProfilingSampler("Depth Height Fog");
        }

        public void Setup(ExponentialHeightFog component)
        {
            m_Component = component;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureInput(ScriptableRenderPassInput.Color); 
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || m_Component == null || !m_Component.IsActive())
                return;

            var cmd = CommandBufferPool.Get();
            
            using (new ProfilingScope(cmd, profilingSampler))
            {
                SetMaterial();
                
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0; 

                cmd.GetTemporaryRT(m_TempRT_ID, descriptor, FilterMode.Bilinear);
                
                cmd.Blit(source, m_TempRT_ID, m_Material, 0);
                cmd.Blit(m_TempRT_ID, source);

                cmd.ReleaseTemporaryRT(m_TempRT_ID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetMaterial()
        {
            // 1. 设置 PostProcess 材质参数
            // -----------------------------------------------------------------------
            // General & Colors
            m_Material.SetFloat(_WaterLevel, m_Component.waterLevel.value);
            m_Material.SetFloat(_StartDistance, m_Component.startDistance.value);
            m_Material.SetColor(_WaterShallowColor, m_Component.waterShallowColor.value);
            m_Material.SetColor(_WaterDeepColor, m_Component.waterDeepColor.value);
            m_Material.SetFloat(_UnderwaterFogBrightness, m_Component.underwaterFogBrightness.value);
            m_Material.SetFloat(_HeightFogBrightness, m_Component.heightFogBrightness.value);
            
            // Density
            m_Material.SetFloat(_UnderwaterFogDensity, m_Component.underwaterFogDensity.value);
            m_Material.SetFloat(_HeightFogDensity, m_Component.heightFogDensity.value);
            m_Material.SetFloat(_HeightFogDepth, m_Component.heightFogDepth.value);

            // Distortion (Local)
            if (m_Component.noiseTexture.value != null)
                m_Material.SetTexture(_NoiseTexture, m_Component.noiseTexture.value);
            m_Material.SetVector(_DistortionSpeed, m_Component.distortionSpeed.value);
            m_Material.SetFloat(_DistortionStrength, m_Component.distortionStrength.value);
            m_Material.SetFloat(_EdgeFade, m_Component.edgeFade.value);

            // Helmet Mask (Local)
            bool helmetOn = m_Component.enableHelmetMask.value;
            if (helmetOn)
            {
                m_Material.EnableKeyword("_HELMET_MASK_ON");
                m_Material.SetColor(_HelmetColor, m_Component.helmetColor.value);
                m_Material.SetFloat(_LensDistortionStrength, m_Component.lensDistortionStrength.value);
                m_Material.SetFloat(_MaskHardness, m_Component.maskHardness.value);
            }
            else
            {
                m_Material.DisableKeyword("_HELMET_MASK_ON");
            }

            // 2. 设置全局变量 (供 FakeLight.shader 等外部 Shader 同步扭曲使用)
            // -----------------------------------------------------------------------
            if (m_Component.noiseTexture.value != null)
            {
                Shader.SetGlobalTexture(_Global_WaterNoiseTexture, m_Component.noiseTexture.value);
            }
            
            Shader.SetGlobalVector(_Global_WaterDistortionSpeed, m_Component.distortionSpeed.value);
            Shader.SetGlobalFloat(_Global_WaterDistortionStrength, m_Component.distortionStrength.value);
            Shader.SetGlobalFloat(_Global_WaterEdgeFade, m_Component.edgeFade.value);
            
            // 面罩全局变量
            Shader.SetGlobalFloat(_Global_HelmetMaskEnabled, helmetOn ? 1.0f : 0.0f);
            Shader.SetGlobalFloat(_Global_LensDistortionStrength, m_Component.lensDistortionStrength.value);
        }
    }
}