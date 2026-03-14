Shader "Custom/GeometryPointMap"
{
    HLSLINCLUDE
    #pragma target 4.5
    #include "UnityCG.cginc"

    struct PointData
    {
        float3 position;
        uint color;
    };

    StructuredBuffer<PointData> _Points;
    
    int _PointCount;
    float4x4 _LocalToWorldMatrix;

    float _MaxHeight;
    float _PointSize;

    float3 _CameraRightWS;
    float3 _CameraUpWS;

    struct Attributes
    {
        float3 vertex : POSITION;
        float2 uv : TEXCOORD0;
        uint instanceID : SV_InstanceID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 worldPos : TEXCOORD0;
        float2 quadUV : TEXCOORD1;
        float3 packedColor : TEXCOORD2;
    };

    float3 HSVtoRGB(float3 c)
    {
        float4 K = float4(1., 2./3., 1./3., 3.);
        float3 p = abs(frac(c.xxx + K.xyz) * 6. - K.www);
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
    }
    
    float3 UnpackColor(uint c)
    {
        float r = (c & 255) / 255.0;
        float g = ((c >> 8) & 255) / 255.0;
        float b = ((c >> 16) & 255) / 255.0;

        return float3(r,g,b);
    }

    Varyings Vert(Attributes IN)
    {
        Varyings OUT;

        uint id = IN.instanceID;
        float3 localPos = _Points[id].position;
        float2 quad = IN.vertex.xy;
        
        float3 worldCenter = mul(_LocalToWorldMatrix, float4(localPos,1)).xyz;
        float3 worldOffset =
            (_CameraRightWS * quad.x + _CameraUpWS * quad.y) * _PointSize;

        float3 worldPos = worldCenter + worldOffset;
        uint packed = _Points[id].color;
        
        OUT.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos,1));
        OUT.worldPos = worldCenter;
        OUT.quadUV = IN.uv;
        OUT.packedColor = UnpackColor(packed);
        return OUT;
    }

    float4 Frag(Varyings IN) : SV_Target
    {
        /*Quad to Point*/
        float2 uv = IN.quadUV * 2 - 1;
        if (dot(uv, uv) > 1)
            discard;
        
        /*Height based*/
        _MaxHeight = 10;
        float h = 1 - IN.worldPos.y / _MaxHeight;

        return float4(h, h, h, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="ForwardOnly" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}