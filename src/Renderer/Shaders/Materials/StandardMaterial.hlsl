// https://learnopengl.com/PBR/Lighting

#include "Common.hlsli"
#include "../Lighting/Lights.hlsli"
#include "../ForwardPlus/Common.hlsli"

SAMPLER2D_PS(Albedo, 0)
SAMPLER2D_PS(Normal, 1)
SAMPLER2D_PS(Metallic, 2)
SAMPLER2D_PS(Roughness, 3)
SAMPLER2D_PS(Occlusion, 4)

StructuredBuffer<Light> SceneLights : register(t5, space2);
StructuredBuffer<uint> LightIndices : register(t6, space2);

cbuffer SceneData : register(b0, space3)
{
    Scene gScene;
}

float4 PSMain(const in VertexOutput input): SV_Target0
{
    const float2 normalizedPos = input.Position.xy / gScene.TargetSize;
    const uint2 numTiles = GetNumberOfTiles(gScene.TargetSize);
    const uint2 currentTile = numTiles * normalizedPos;
    const uint currentTileIndex = currentTile.y * numTiles.x + currentTile.x;
    const uint startOffset = currentTileIndex * MAX_LIGHTS_PER_TILE;
        
    uint numLightsInThisTile = 0;
    /*for (int i = 0; i < MAX_LIGHTS_PER_TILE; i++)
    {
        const uint lightIndex = LightIndices[startOffset + i];
        if (lightIndex == LIGHT_BUFFER_END_OF_ARRAY)
            break;
        
        numLightsInThisTile++;
    }*/
    
    //return float4(currentTileIndex / (float) (numTiles.x * numTiles.y), 0.0, 0.0, 1.0);
    
    float3 albedo = SAMPLE(Albedo, input.TexCoord).rgb * (float3) input.Color;
    // Sample the metallic texture's blue channel, and the roughness texture's green channel.
    // Most Metallic-Roughness textures will provide the same color on each of the RGB channels,
    // however this also allows for glTF metallic-roughness textures (which are combined into one) to work as well.
    const float metallic = SAMPLE(Metallic, input.TexCoord).b;
    const float roughness = SAMPLE(Roughness, input.TexCoord).g;
    const float ao = SAMPLE(Occlusion, input.TexCoord).r;
    const float3 normal = GetNormal(SAMPLE(Normal, input.TexCoord).rgb, input.Normal, input.TexCoord, input.WorldPos);
    const float3 view = normalize((float3) gScene.Camera.Position - input.WorldPos);
    
    float3 light = DirectionalLight(float2(0, -M_PI / 2), (float3) 1.0, view, albedo, normal, metallic, roughness);
    //float3 light = (float3) 0;
    
    const float power = 800;
    const float radius = 50;
    /*light += PointLight(float3(0, 1, 0), float3(1, 0, 0), power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
    light += PointLight(float3(-8, 1, -8), float3(0, 1, 0), power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
    light += PointLight(float3(8, 1, -8), float3(0, 0, 1), power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
    light += PointLight(float3(8, 1, 8), float3(1, 1, 0), power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
    light += PointLight(float3(-8, 1, 8), float3(0, 1, 1), power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);*/
    
    if (gScene.UseLightIndices)
    {
        for (int i = 0; i < MAX_LIGHTS_PER_TILE; i++)
        {
            const uint lightIndex = LightIndices[startOffset + i];
            if (lightIndex == LIGHT_BUFFER_END_OF_ARRAY)
                break;
            const Light l = SceneLights[lightIndex];
            //Light l = SceneLights[i];
            light += PointLight(l.Position.xyz, l.Color.xyz, power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
        }
    }
    else
    {
        for (int i = 0; i < gScene.NumLights; i++)
        {
            const Light l = SceneLights[i];
            light += PointLight(l.Position.xyz, l.Color.xyz, power, radius, input.WorldPos, view, albedo, normal, metallic, roughness);
        }
    }
    
    const float3 ambient = (float3) 0.03 * albedo * ao;
    float3 color = ambient + light;
    color /= color + (float3) 1.0;
    color = pow(color, (float3) 1.0 / 2.2);
    
    return float4(color + float3(numLightsInThisTile / (float) gScene.NumLights, 0, 0), 1.0);
}