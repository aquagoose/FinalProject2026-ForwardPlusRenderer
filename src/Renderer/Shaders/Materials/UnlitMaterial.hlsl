#include "Common.hlsli"

SAMPLER2D_PS(Texture, 0)

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return SAMPLE(Texture, input.TexCoord) * input.Color;
}