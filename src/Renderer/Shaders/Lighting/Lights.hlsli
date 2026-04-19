#ifndef LIGHTING_LIGHTS_H
#define LIGHTING_LIGHTS_H

#define LIGHT_TYPE_POINT 1

#include "BRDF.hlsli"

struct Light
{
    uint LightType;
    float4 Position;
    float4 Color;
};

float3 CalculateLight(const float3 lightVector, const float3 radiance, const float3 view, const float3 albedo,
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
    return CalculateLight(l, color, view, albedo, normal, metallic, roughness);
}

float3 PointLight(const float3 position, const float3 color, const float power, const float radius,
    const float3 worldPos, const float3 view, const float3 albedo, const float3 normal, const float3 metallic,
    const float roughness)
{
    const float3 pos = position - worldPos;
    const float3 lightVector = normalize(pos);
    const float distance2 = dot(pos, pos);
    //const float distance2 = distance * distance;
    /*const float numerator = saturate(1 - pow((distance / radius), 4));
    const float denominator = (distance2 + 1);
    const float falloff = (numerator * numerator) / denominator;
    const float3 radiance = color * falloff * power;*/
    
    const float nDotL = max(dot(normal, lightVector), 0.0);
    const float intensity = (power / (4 * M_PI * distance2));
    
    const float invRadius = 1.0 / radius;
    const float factor = distance2 * invRadius * invRadius;
    const float smoothFactor = max(1.0 - factor * factor, 0.0);
    const float falloffAttenuation = (smoothFactor * smoothFactor) / max(distance2, 1e-4);
    
    // TODO: Radius doesn't work. Fix that
    const float3 radiance = intensity * nDotL * color;
    
    return CalculateLight(lightVector, radiance, view, albedo, normal, metallic, roughness);
}

#endif