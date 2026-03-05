Shader "Custom/PointCloudHDRP"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Name "Forward"

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct PointXYZRGB
            {
                float3 pos;
                uint color;
            };

            StructuredBuffer<PointXYZRGB> _Points;

            float _PointSize;

            struct Attributes
            {
                float3 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            float4 UnpackRGBA8(uint c)
            {
                float r = (c & 255) / 255.0;
                float g = ((c >> 8) & 255) / 255.0;
                float b = ((c >> 16) & 255) / 255.0;
                float a = ((c >> 24) & 255) / 255.0;
                return float4(r,g,b,a);
            }

            Varyings vert(Attributes input)
            {
                Varyings o;

                PointXYZRGB p = _Points[input.instanceID];

                float3 worldPos = p.pos + input.positionOS * _PointSize;

                o.positionCS = TransformWorldToHClip(worldPos);
                o.color = UnpackRGBA8(p.color);

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return i.color;
            }

            ENDHLSL
        }
    }
}