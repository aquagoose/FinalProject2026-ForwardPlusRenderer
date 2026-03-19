#include "BaseVertex.hlsl"

SAMPLER2D_FS(Texture, 0)

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return SAMPLE(Texture, input.TexCoord) * input.Color;
}