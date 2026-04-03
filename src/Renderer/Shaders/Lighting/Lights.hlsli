#ifndef LIGHTING_LIGHTS_H
#define LIGHTING_LIGHTS_H

#include "BRDF.hlsli"

float3 Light(const float3 lightVector, const float3 radiance, const float3 view, const float3 albedo,
             const float3 normal, const float3 metallic, const float roughness)
{
    const float3 h = normalize(view + lightVector);
    const float3 f0 = lerp((float3) 0.04, albedo, metallic);
    
    const float ndf = SpecularD(roughness, normal, h);
    const float g = SpecularG(roughness, lightVector, normal, view);
    const float3 f = SpecularF(f0, view, h);
    
    const float3 kS = f;
    float3 kD = (float3) 1.0 - kS;
    kD *= 1.0 - metallic;
    
    const float3 brdf = BRDF(lightVector, normal, view, ndf, g, f);
    
    const float nDotL = max(dot(normal, lightVector), 0.0);
    const float3 light = (kD * albedo / M_PI + brdf) * radiance * nDotL;
    
    return light;
}

float3 DirectionalLight(const float2 direction, const float3 color, const float3 view, const float3 albedo,
                        const float3 normal, const float3 metallic, const float roughness)
{
    // Convert Yaw-Pitch vector into cartesian direction vector
    const float yaw = direction.x;
    const float pitch = direction.y;  
    
    const float sinYaw = sin(yaw);
    const float cosYaw = cos(yaw);
    const float sinPitch = sin(pitch);
    const float cosPitch = cos(pitch);
    
    const float x = sinYaw * sinPitch;
    const float y = cosYaw * sinPitch;
    const float z = cosPitch;
    
    const float3 lightDir = float3(x, y, z);
    const float3 l = normalize(-lightDir);
    
    // The radiance of a directional light is just equal to its color.
    return Light(l, color, view, albedo, normal, metallic, roughness);
}

#endif