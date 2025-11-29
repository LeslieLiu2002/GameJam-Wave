using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Post-processing/Custom Exponential Height Fog")]
public sealed class ExponentialHeightFog : VolumeComponent, IPostProcessComponent
{
    public BoolParameter enabled = new BoolParameter(false);

    [Header("General")]
    [Tooltip("水面高度 (Y轴世界坐标)")]
    public FloatParameter waterLevel = new FloatParameter(0.0f);

    [Tooltip("雾开始的距离 (相对于摄像机)")]
    public FloatParameter startDistance = new FloatParameter(0.0f);

    [Header("Colors")]
    [Tooltip("浅水颜色")]
    public ColorParameter waterShallowColor = new ColorParameter(new Color(0.3f, 0.8f, 0.9f, 1f));
    
    [Tooltip("深水颜色")]
    public ColorParameter waterDeepColor = new ColorParameter(new Color(0.0f, 0.1f, 0.3f, 1f));
    
    [Tooltip("距离雾的亮度倍增")]
    public FloatParameter underwaterFogBrightness = new FloatParameter(1.0f);
    
    [Tooltip("高度雾的亮度倍增")]
    public FloatParameter heightFogBrightness = new FloatParameter(1.0f);

    [Header("Density")]
    [Tooltip("距离雾的密度 (基于水平距离)")]
    public ClampedFloatParameter underwaterFogDensity = new ClampedFloatParameter(0.05f, 0.0f, 1.0f);

    [Tooltip("高度雾的密度 (基于深度积分)")]
    public ClampedFloatParameter heightFogDensity = new ClampedFloatParameter(0.5f, 0.0f, 5.0f);

    [Tooltip("高度雾的计算偏移")]
    public FloatParameter heightFogDepth = new FloatParameter(0.0f);

    [Header("Distortion")]
    [Tooltip("扰动贴图 (建议使用无缝噪声图)")]
    public TextureParameter noiseTexture = new TextureParameter(null);

    [Tooltip("扰动速度 (XY: Layer1, ZW: Layer2)")]
    public Vector4Parameter distortionSpeed = new Vector4Parameter(new Vector4(0.05f, 0.05f, -0.05f, -0.05f));

    [Tooltip("扰动强度")]
    public ClampedFloatParameter distortionStrength = new ClampedFloatParameter(0.02f, 0.0f, 0.2f);

    [Tooltip("边缘遮罩淡入范围")]
    public ClampedFloatParameter edgeFade = new ClampedFloatParameter(0.1f, 0.001f, 1.0f);

    [Header("Helmet Mask (面罩)")]
    [Tooltip("开启面罩模式 (包含透镜畸变和视野遮挡)")]
    public BoolParameter enableHelmetMask = new BoolParameter(false);

    [Tooltip("面罩遮挡部分的颜色")]
    public ColorParameter helmetColor = new ColorParameter(Color.black);

    [Tooltip("面罩透镜的畸变强度")]
    public ClampedFloatParameter lensDistortionStrength = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);

    [Tooltip("面罩边缘的硬度 (值越大边缘越硬)")]
    public MinFloatParameter maskHardness = new MinFloatParameter(400.0f, 1.0f);

    public bool IsActive() => enabled.value && (underwaterFogDensity.value > 0.0f || heightFogDensity.value > 0.0f);
    
    public bool IsTileCompatible() => false;
}