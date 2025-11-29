Shader "Wave/3DScanSphere"
{
    Properties 
    {
       _MainColor ("Scan Color", Color) = (0, 0.8, 1, 1)
       // 控制球体内部空心的大小 (0 ~ 0.5)
       _MinRange ("Inner Hollow Radius", Range(0, 0.5)) = 0.1
       
       // 控制波纹层的锐利度，越大越细
       _Power ("Layer Sharpness", float) = 20
       // 总体系数
       _Strength ("Total Strength", float) = 1

       // 扩散速度，负数向内，正数向外
       _ScanSpeed ("Expansion Speed", float) = -0.5
       // 层的密度，值越大，层数越多
       _LayerDensity ("Layer Density", float) = 10
    }
    
    Subshader 
    {
       Tags 
       { 
          "RenderType"="Transparent" 
          "Queue"="Transparent+100" // 稍微延后渲染以确保在其他透明物体之上
          "IgnoreProjector"="true" 
          "DisableBatching"="true"
       }

       Pass 
       {
          // 标准透明混合设定
          ZWrite Off
          Blend SrcAlpha OneMinusSrcAlpha
          // 剔除正面，只渲染背面。
          // 这样当摄像机进入球体内部时，依然能看到球体效果。
          Cull Front 
          
          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          #include "UnityCG.cginc"
          
          // 输入结构体
          struct a2v
          {
                float4 vertex : POSITION;
          };

          // 输出到片元结构体
          struct v2f
          {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 ray : TEXCOORD1;
          };
                   
          fixed4 _MainColor;
          // 注意：这里必须声明摄像机深度图
          sampler2D _CameraDepthTexture; 
          float _MinRange;
          float _Power;
          float _Strength;
          float _ScanSpeed;
          float _LayerDensity;

          v2f vert (a2v v)
          {
             v2f o;
             // 转裁切空间坐标
             o.vertex = UnityObjectToClipPos(v.vertex);
             // 计算屏幕坐标用于采样深度图
             o.screenPos = ComputeScreenPos(o.vertex);
             // 计算从相机到顶点的射线（世界空间）
             o.ray = mul(unity_ObjectToWorld, v.vertex) - _WorldSpaceCameraPos;
             return o;
          }

          fixed4 frag (v2f i) : SV_Target
          {
             // --- 1. 深度重构世界坐标 (核心原理不变) ---
             fixed2 screenUV = i.screenPos.xy / i.screenPos.w;
             // 获取线性深度值
             float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV).r);
             
             // 修正射线方向，使其长度对应 Z 轴深度
             float3 worldRay = normalize(i.ray);
             worldRay /= dot(worldRay, -UNITY_MATRIX_V[2].xyz);
             
             // 重建像素点的世界坐标
             float3 worldPos = _WorldSpaceCameraPos + worldRay * depth;
             
             // --- 2. 转为局部坐标并计算 3D 距离 (关键修改) ---
             // 将世界坐标转回当前 Cube 的局部坐标
             float3 objectPos = mul(unity_WorldToObject, float4(worldPos,1)).xyz;

             // [核心改动]：计算 3D 球体距离！
             // 原来是 length(objectPos.xy)，现在是 length(objectPos.xyz)
             float sphereDist = length(objectPos);

             // --- 3. 球体裁剪与范围控制 ---
             // 如果距离大于 0.5 (Cube 的最大内切球半径)，则丢弃。
             // 这让效果被限制在一个完美的球体内，而不是原来的立方体内。
             clip(0.5 - sphereDist);
             
             // 计算内圈空心范围 (MinRange)
             // 如果 sphereDist 小于 _MinRange，step 返回 0，挖空中心。
             float rangeMask = step(_MinRange, sphereDist);

             // --- 4. 程序化生成三维层级波纹 (Procedural 3D Layers) ---
             
             // 基础动画：距离随时间偏移
             // 使用 _Time.y 驱动动画，_ScanSpeed 控制方向和速度
             float animatedDist = sphereDist + _Time.y * _ScanSpeed;

             // 将距离映射到层级密度域
             // 乘以密度系数，使得在 0-0.5 的距离内出现多个周期
             float layerDomain = animatedDist * _LayerDensity;

             // [核心算法]：生成循环波形
             // frac(x) 等同于 x - floor(x)，产生一个 0 -> 1 的锯齿波
             float sawtoothWave = frac(layerDomain);

             // [波形塑形]：将锯齿波塑形为脉冲
             // 这里使用 pow(1 - x, power) 来创建一个前面陡峭、后面拖尾的声纳波形效果
             // 你也可以尝试用 sin 函数来做更柔和的呼吸效果
             float layerPulse = pow(1.0 - sawtoothWave, _Power);

             // --- 5. 组合最终颜色 ---
             // 基础颜色 * 范围遮罩 * 波纹脉冲 * 总强度
             float finalAlpha = _MainColor.a * rangeMask * layerPulse * _Strength;
             
             // 可选：在球体边缘增加一点柔和衰减，防止切边太硬
             finalAlpha *= smoothstep(0.5, 0.45, sphereDist);

             return fixed4(_MainColor.rgb, finalAlpha);
          }
          ENDCG
       }
    }
}