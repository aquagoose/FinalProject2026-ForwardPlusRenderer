#ifndef MATERIALS_COMMON_H
#define MATERIALS_COMMON_H

#include "../Common.hlsli"

struct VertexOutput
{
    float4 Position: SV_Position;
    float3 WorldPos: POSITION0;
    float2 TexCoord: TEXCOORD0;
    float4 Color:    COLOR0;
    float3 Normal:   NORMAL0;
};

// HLSL implementation of getNormalFromMap() in
// https://learnopengl.com/code_viewer_gh.php?code=src/6.pbr/1.2.lighting_textured/1.2.pbr.fs
float3 GetNormal(const float3 normalMap, const float3 normal, const float2 texCoord, const float3 worldPos)
{
    const float3 tangentNormal = normalMap * 2.0 - 1.0;
    
    const float3 q1 = ddx(worldPos);
    const float3 q2 = ddy(worldPos);
    const float2 uv1 = ddx(texCoord);
    const float2 uv2 = ddy(texCoord);
    
    const float3 n = normalize(normal);
    const float3 t = normalize(q1 * uv2.y - q2 * uv1.y);
    const float3 b = -normalize(cross(n, t));
    const float3x3 tbn = float3x3(t, b, n);
    
    return normalize(mul(tangentNormal, tbn));
}

#endif