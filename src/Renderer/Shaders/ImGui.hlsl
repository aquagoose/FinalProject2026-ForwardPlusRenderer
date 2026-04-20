#pragma vertex VSMain 1 0
#pragma pixel PSMain 0 1

#include "Common.hlsli"

struct VSInput
{
    float2 Position: TEXCOORD0;
    float2 TexCoord: TEXCOORD1;
    float4 Color: TEXCOORD2;
};

struct VSOutput
{
    float4 Position: SV_Position;
    float2 TexCoord: TEXCOORD0;
    float4 Color: COLOR0;
};

struct PSOutput
{
    float4 Color: SV_Target0;
};

cbuffer ProjectionBuffer : register(b0, space1)
{
    float4x4 Projection;
}

SAMPLER2D_PS(Texture, 0)

VSOutput VSMain(const in VSInput input)
{
    VSOutput output;

    output.Position = mul(Projection, float4(input.Position, 0.0, 1.0));
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;

    return output;
}

PSOutput PSMain(const in VSOutput input)
{
    PSOutput output;

    output.Color = SAMPLE(Texture, input.TexCoord) * input.Color;
    
    return output;
}