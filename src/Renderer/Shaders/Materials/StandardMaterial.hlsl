// https://learnopengl.com/PBR/Lighting

#include "Common.hlsli"
#include "../Lighting/BRDF.hlsli"

SAMPLER2D_PS(Albedo, 0)
SAMPLER2D_PS(Normal, 1)
SAMPLER2D_PS(Metallic, 2)
SAMPLER2D_PS(Roughness, 3)
SAMPLER2D_PS(Occlusion, 4)

cbuffer CameraData : register(b0, space3)
{
    Camera gCamera;
}

float4 PSMain(const in VertexOutput input): SV_Target0
{
    float3 albedo = SAMPLE(Albedo, input.TexCoord).rgb * (float3) input.Color;
    const float metallic = SAMPLE(Metallic, input.TexCoord).r;
    const float roughness = SAMPLE(Roughness, input.TexCoord).r;
    const float ao = SAMPLE(Occlusion, input.TexCoord).r;
    const float3 normal = GetNormal(SAMPLE(Normal, input.TexCoord).rgb, input.Normal, input.TexCoord, input.WorldPos);
    
    const float3 view = normalize((float3) gCamera.Position - input.WorldPos);
    
    const float3 lightPos = float3(0.0, 1.0, 1.0);
    const float3 l = normalize(lightPos - input.WorldPos);
    const float3 h = normalize(view + l);
    const float distance = length(lightPos - input.WorldPos);
    const float attenuation = 1.0 / (distance * distance);
    const float3 radiance = (float3) 1.0 * attenuation;
    
    const float3 f0 = lerp((float3) 0.04, albedo, metallic);
    
    const float ndf = SpecularD(roughness, normal, h);
    const float g = SpecularG(roughness, l, normal, view);
    const float3 f = SpecularF(f0, view, h);
    
    const float3 kS = f;
    float3 kD = (float3) 1.0 - kS;
    kD *= 1.0 - metallic;
    
    const float3 numerator = ndf * g * f;
    const float3 denominator = 4.0 * max(dot(normal, view), 0.0) * max(dot(normal, l), 0.0) + 0.0001;
    const float3 specular = numerator / denominator;
    
    //const float3 brdf = BRDF(roughness, l, normal, view, h);
    
    const float nDotL = max(dot(normal, l), 0.0);
    const float3 light = (kD * albedo / M_PI + specular) * radiance * nDotL;
    //const float3 light = specular;
    
    const float3 ambient = (float3) 0.03 * albedo * ao;
    float3 color = ambient + light;
    color /= color + (float3) 1.0;
    color = pow(color, (float3) 1.0 / 2.2);
    
    return float4(color, 1.0);
}