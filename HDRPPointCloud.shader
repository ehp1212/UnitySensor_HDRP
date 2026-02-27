Shader "Custom/HDRPPointCloud"
{
    Properties
    {
        _SensorPosition ("Sensor Position", Vector) = (0, 0, 0)
        _MaxDistance ("Max Distance", Float) = 50
        _PointSize ("Point Size", Float) = 2
    }

    HLSLINCLUDE
    #pragma target 4.5
    #include "UnityCG.cginc"
    StructuredBuffer<float4> PointsBuffer;
    float4x4 LocalToWorldMatrix;

    float _MinDistance;
    float _MaxDistance;
    float _PointSize;

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

        float3 localPos = PointsBuffer[IN.instanceID].xyz;
        float3 worldPos = mul(LocalToWorldMatrix, float4(localPos, 1.0)).xyz;
        OUT.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
        
        OUT.worldPos = worldPos;

        return OUT;
    }

    float4 Frag(Varyings IN) : SV_Target
    {
        float distance = length(IN.worldPos - _SensorPosition);
        float t = saturate(distance / _MaxDistance);

        float3 color;
        if (t < 0.25)
            color = float3(1,0,0);      // red
        else if (t < 0.75)
            color = lerp(float3(1,0,0), float3(0,1,1), t);
        else
            color = float3(0,1,1);      // blue        

        return float4(color,1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="ForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}