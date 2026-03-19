#include "BaseVertex.hlsl"

SAMPLER2D_FS(Albedo, 0)
SAMPLER2D_FS(Normal, 1)
SAMPLER2D_FS(Metallic, 2)
SAMPLER2D_FS(Roughness, 3)
SAMPLER2D_FS(Occlusion, 4)

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return SAMPLE(Albedo, input.TexCoord) * input.Color;
}