#include "BaseVertex.hlsl"

float4 PSMain(const in VertexOutput input): SV_Target0
{
    return input.Color;
}