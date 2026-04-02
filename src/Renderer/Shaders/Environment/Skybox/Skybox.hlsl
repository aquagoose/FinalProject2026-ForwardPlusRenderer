#include "../../Common.hlsli"

struct VertexOutput
{
    float4 Position: SV_Position;
    float3 TexCoord: TEXCOORD0;
};

cbuffer CameraMatrices : register(b0, space1)
{
    Camera gCamera;
}

SAMPLERCUBE_PS(Cube, 0)

VertexOutput VSMain(const in float3 position: TEXCOORD0)
{
    VertexOutput output;
    
    // Cast view matrix to 3x3 matrix to remove all translation information
    output.Position = mul(gCamera.Projection, mul(gCamera.View, float4(position, 1.0))).xyww;
    output.TexCoord = position;
    
    return output;
}

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return SAMPLE(Cube, input.TexCoord);
}