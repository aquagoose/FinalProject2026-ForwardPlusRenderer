// Cook-Torrance BRDF
// https://cdn2.unrealengine.com/Resources/files/2013SiggraphPresentationsNotes-26915738.pdf
// https://learnopengl.com/PBR/Lighting
// LEGEND:
//   - V: Normalized view vector
//   - L: Normalized light vector
//   - N: Normal vector

#ifndef LIGHTING_BRDF_H
#define LIGHTING_BRDF_H

#include "../Math.hlsli"

float SpecularD(const float roughness, const float3 n, const float3 h)
{
    const float alpha = roughness * roughness;
    const float alpha2 = alpha * alpha;
    
    const float nDotH = max(dot(n, h), 0.0);
    const float nDotH2 = nDotH * nDotH;
    
    const float numerator = alpha2;
    float denominator = (nDotH2 * (alpha2 - 1.0)) + 1.0;
    denominator = M_PI * denominator * denominator;
    
    return numerator / denominator;
}

float G(const float k, const float3 n, const float3 v)
{
    const float nDotV = max(dot(n, v), 0.0);
    const float denominator = (nDotV * (1.0 - k)) + k;
    
    return nDotV / denominator;
}

float SpecularG(const float roughness, const float3 l, const float3 n, const float3 v)
{
    // Remap roughness as (roughness + 1) / 2
    float k = ((roughness + 1.0) / 2.0) + 1.0;
    k = (k * k) / 8;
    
    return G(k, n, v) * G(k, n, l);
}

float3 SpecularF(const float3 f0, const float3 v, const float3 h)
{
    const float vDotH = max(dot(v, h), 0.0);
    return f0 + (1.0 - f0) * pow(clamp(1.0 - vDotH, 0.0, 1.0), 5.0);
}

float3 BRDF(const float roughness, const float3 l, const float3 n, const float3 v, const float3 h)
{
    const float d = SpecularD(roughness, n, h);
    const float3 f = SpecularF(0.04, v, h);
    const float3 g = SpecularG(roughness, l, n, v);
    
    const float nDotL = dot(n, l);
    const float nDotV = dot(n, v);
    
    const float3 numerator = d * f * g;
    const float denominator = 4 * nDotL * nDotV;
    
    return numerator / denominator;
}

#endif