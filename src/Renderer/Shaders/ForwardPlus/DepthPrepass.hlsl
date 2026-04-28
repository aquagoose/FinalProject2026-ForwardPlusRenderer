#include "../Common.hlsli"

struct Vertex
{
    float3 Position: TEXCOORD0;
};

cbuffer SceneData : register(b0, space1)
{
    Scene gScene;
}

cbuffer DrawData : register(b1, space1)
{
    float4x4 World;
}

float4 VSMain(const in Vertex input): SV_Position
{
    return mul(gScene.Camera.Projection, mul(gScene.Camera.View, mul(World, float4(input.Position, 1.0))));
}

float4 PSMain(): SV_Target0
{
    return 1.0;
}