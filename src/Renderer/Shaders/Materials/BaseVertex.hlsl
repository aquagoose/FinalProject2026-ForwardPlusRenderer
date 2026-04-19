#include "../Common.hlsli"
#include "Common.hlsli"

struct Vertex
{
    float3 Position: TEXCOORD0;
    float2 TexCoord: TEXCOORD1;
    float4 Color:    TEXCOORD2;
    float3 Normal:   TEXCOORD3;
};

cbuffer SceneData : register(b0, space1)
{
    Scene gScene;
}

cbuffer DrawData : register(b1, space1)
{
    float4x4 World;
}

cbuffer TempMatrixData : register(b2, space1)
{
    float4x4 NormalMatrix;
}

VertexOutput VSMain(const in Vertex input)
{
    VertexOutput output;
    
    output.WorldPos = (float3) mul(World, float4(input.Position, 1.0));
    output.Position = mul(gScene.Camera.Projection, mul(gScene.Camera.View, float4(output.WorldPos, 1.0)));
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.Normal = mul((float3x3) NormalMatrix, input.Normal);
    
    return output;
}