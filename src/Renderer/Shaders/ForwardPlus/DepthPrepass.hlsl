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
    float4 value = mul(gScene.Camera.Projection, mul(gScene.Camera.View, mul(World, float4(input.Position, 1.0))));
    value.z += 1e-5; // For some reason on macos the color pass and depth pass have precision issues and so you get
                     // graphical glitches. Adding an epsilon value seems to fix it. I don't like it but here we are
    return value;
}

float4 PSMain(): SV_Target0
{
    return 1.0;
}