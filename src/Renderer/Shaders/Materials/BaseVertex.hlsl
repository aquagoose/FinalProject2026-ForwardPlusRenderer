#include "../Common.hlsli"

struct Vertex
{
    float3 Position: TEXCOORD0;
    float2 TexCoord: TEXCOORD1;
    float4 Color:    TEXCOORD2;
};

struct VertexOutput
{
    float4 Position: SV_Position;
    float2 TexCoord: TEXCOORD0;
    float4 Color:    COLOR0;
};

cbuffer CameraData : register(b0, space1)
{
    Camera gCamera;
}

cbuffer DrawData : register(b1, space1)
{
    float4x4 World;
}

VertexOutput VSMain(const in Vertex input)
{
    VertexOutput output;
    
    output.Position = mul(gCamera.Projection, mul(gCamera.View, mul(World, float4(input.Position, 1.0))));
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    
    return output;
}