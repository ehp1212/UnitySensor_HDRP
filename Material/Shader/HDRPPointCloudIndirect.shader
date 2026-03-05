Shader "Custom/HDRPPointCloudIndirect"
{
    HLSLINCLUDE
    #pragma target 4.5
    #include "UnityCG.cginc"
    StructuredBuffer<float4> PointsBuffer;
    float4x4 LocalToWorldMatrix;

    float _MaxDistance;
    
    float _PointSize;
    float3 _CameraRightWS;
    float3 _CameraUpWS;

    float _MinRedDistance; 
    float3 _SensorPosition;
    
    struct Attributes
    {
        float3 vertex : POSITION;   // quad vertex
        uint instanceID : SV_InstanceID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 worldPos : TEXCOORD0;
    };

    float3 HSVtoRGB(float3 c)
    {
        float4 K = float4(1., 2./3., 1./3., 3.);
        float3 p = abs(frac(c.xxx + K.xyz) * 6. - K.www);
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
    }

    Varyings Vert(Attributes IN)
    {
        Varyings OUT;

        float3 localPos = PointsBuffer[IN.instanceID];
        float2 quad = IN.vertex.xy;
      
        float3 worldCenter = mul(LocalToWorldMatrix, float4(localPos,1)).xyz;
        float3 worldOffset =
            (_CameraRightWS * quad.x + _CameraUpWS * quad.y) * _PointSize;

        float3 worldPos = worldCenter + worldOffset;

        OUT.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos,1));
        OUT.worldPos = worldCenter; // 컬러는 중심 기준

        return OUT;
    }

    float4 Frag(Varyings IN) : SV_Target
    {
        float distance = length(IN.worldPos - _SensorPosition);
        // Force red in min distance
        if (distance < _MinRedDistance)
        {
            return float4(1,0,0,1); 
        }
        
        // RViz Distance Hue Mapping
        float t = saturate(distance / _MaxDistance);
        float hue = t * 0.7;
        float3 color = HSVtoRGB(float3(hue, 1.0, 1.0));

        return float4(color, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual
            
            Name "Forward"
            Tags { "LightMode"="ForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}