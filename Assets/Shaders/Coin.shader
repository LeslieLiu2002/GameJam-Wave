Shader "Unlit/URP_Emission_Lerp_Rotate"
{
    Properties
    {
        [Header(Base Settings)]
        // 基础纹理（A通道用于Mask）
        _MainTex ("Texture (Alpha is Mask)", 2D) = "white" {}
        
        // 基础颜色（背景色）
        _BaseColor ("Base Color", Color) = (0.1, 0.1, 0.1, 1)
        
        // HDR 装饰色（用于高亮部分，支持强度超过1）
        [HDR] _EmissionColor ("Emission Color (HDR)", Color) = (2, 2, 2, 1)

        [Space(10)]
        [Header(Rotation Settings)]
        // 旋转速度 (正数顺时针，负数逆时针，0为静止)
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        // 旋转轴 (例如: Y轴旋转设为 0,1,0)
        _RotationAxis ("Rotation Axis", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags { 
            "Queue" = "Transparent"
            "RenderType"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off 

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct appdata
            {
                float4 vertex  : POSITION;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv         : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                // 新增变量
                float _RotationSpeed;
                float4 _RotationAxis;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // --- 旋转辅助函数 (罗德里格旋转公式) ---
            float3 RotateAroundAxis(float3 position, float3 axis, float angle)
            {
                // 确保轴是归一化的
                axis = normalize(axis);
                float s = sin(angle);
                float c = cos(angle);
                float oc = 1.0 - c;

                // Rodrigues' rotation formula
                return position * c + 
                       cross(axis, position) * s + 
                       axis * dot(axis, position) * oc;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. 计算旋转角度
                // _Time.y 是游戏运行时间（秒）
                float angle = _Time.y * _RotationSpeed;

                // 2. 应用旋转 (在对象空间 Object Space 进行)
                // 注意：如果 _RotationAxis 为 (0,0,0)，normalize 会出错，建议材质中默认给一个值
                float3 rotatedVertex = v.vertex.xyz;
                
                // 只有当设置了旋转轴且速度不为0时才计算，避免无效运算
                if (length(_RotationAxis.xyz) > 0.0 && abs(_RotationSpeed) > 0.0)
                {
                    rotatedVertex = RotateAroundAxis(v.vertex.xyz, _RotationAxis.xyz, angle);
                }

                // 3. 获取裁剪空间坐标 (使用旋转后的顶点位置)
                VertexPositionInputs vertexInput = GetVertexPositionInputs(rotatedVertex);
                o.positionCS = vertexInput.positionCS;

                // 处理 UV 坐标
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 texData = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float mask = texData.a;
                float3 finalColor = lerp(_BaseColor.rgb, _EmissionColor.rgb, mask);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}