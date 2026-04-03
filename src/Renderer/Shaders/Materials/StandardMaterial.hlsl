// https://learnopengl.com/PBR/Lighting

#include "Common.hlsli"
#include "../Lighting/Lights.hlsli"

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
    
    const float3 light = DirectionalLight(float2(0, -M_PI / 2), (float3) 1.0, view, albedo, normal, metallic, roughness);
    
    const float3 ambient = (float3) 0.03 * albedo * ao;
    float3 color = ambient + light;
    color /= color + (float3) 1.0;
    color = pow(color, (float3) 1.0 / 2.2);
    
    return float4(color, 1.0);
}