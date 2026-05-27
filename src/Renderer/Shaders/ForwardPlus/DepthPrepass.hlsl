#include "../Common.hlsli"

struct Vertex
{
    float3 Position: TEXCOORD0;
};

StructuredBuffer<Object> PerObjectData : register(t0, space0);

cbuffer SceneData : register(b0, space1)
{
    Scene gScene;
}

cbuffer DrawData : register(b1, space1)
{
    uint ObjectIndex;
}

float4 VSMain(const in Vertex input): SV_Position
{
    Object object = PerObjectData[ObjectIndex];
    
    float4 value = mul(gScene.Camera.Projection, mul(gScene.Camera.View, mul(object.WorldMatrix, float4(input.Position, 1.0))));
    value.z += 1e-5; // For some reason on macos the color pass and depth pass have precision issues and so you get
                     // graphical glitches. Adding an epsilon value seems to fix it. I don't like it but here we are
    return value;
}

void PSMain() { } // SDL GPU doesn't let you not define a fragment shader. But turns out you can just make it do nothing!