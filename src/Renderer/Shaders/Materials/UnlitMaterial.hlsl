#include "BaseVertex.hlsl"

Texture2D Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return Texture.Sample(Sampler, input.TexCoord) * input.Color;
}