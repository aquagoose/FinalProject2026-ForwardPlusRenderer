#include "Common.hlsli"

SAMPLER2D_PS(Texture, 0)

// https://digitalrune.github.io/DigitalRune-Documentation/html/fa431d48-b457-4c70-a590-d44b0840ab1e.htm
static const float BayerMatrix[4][4] =
{
    { 1.0,  9.0,  3.0, 11.0 },
    { 13.0,  5.0, 15.0,  7.0 },
    { 4.0, 12.0,  2.0, 10.0 },
    { 16.0,  8.0, 14.0,  6.0 }
};

float GetBayerValue(const float2 position)
{
    return (1.0 / 17.0) * BayerMatrix[position.x % 4][position.y % 4];
}

float4 PSMain(const in VertexOutput input): SV_Target0
{
    float4 sampled = SAMPLE(Texture, input.TexCoord) * input.Color;
    clip(sampled.a - GetBayerValue(input.Position.xy));
    
    return sampled;
}