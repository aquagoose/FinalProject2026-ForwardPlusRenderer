// Light culling compute shader
// https://github.com/GPUOpen-LibrariesAndSDKs/ForwardPlus11

#include "../Lighting/Lights.hlsli"
#include "Common.hlsli"

#define NUM_THREADS_PER_TILE TILE_SIZE * TILE_SIZE

//Texture2D<float> DepthTexture : register(t0, space0);
StructuredBuffer<Light> SceneLights : register(t5, space0);

groupshared uint LightIndexCounter;
groupshared uint LightIndices[MAX_LIGHTS_PER_TILE];

cbuffer SceneData : register(b0, space2)
{
    Scene gScene;
}

// Calculate the number of tiles. This adds one extra tile if the screen size does not cleanly divide by TILE_SIZE.
uint2 GetNumberOfTiles()
{
    const uint x = (uint) ((gScene.TargetSize.x + TILE_SIZE - 1) / (float) TILE_SIZE);
    const uint y = (uint) ((gScene.TargetSize.y + TILE_SIZE - 1) / (float) TILE_SIZE);
    return uint2(x, y);
}

float3 CreatePlaneEquation(float3 b, float3 c)
{
    return normalize(cross(b, c));
}

float4 ConvertProjToView(float4 p)
{
    return mul(gScene.Camera.View, p);
}

float GetSignedDistanceFromPlane(const float3 p, const float3 equation)
{
    return dot(equation, p);
}

bool TestFrustumSides(const float3 center, const float radius, const float3 planes[4])
{
    const bool intersectingOrInside0 = GetSignedDistanceFromPlane(center, planes[0]) < radius;
    const bool intersectingOrInside1 = GetSignedDistanceFromPlane(center, planes[1]) < radius;
    const bool intersectingOrInside2 = GetSignedDistanceFromPlane(center, planes[2]) < radius;
    const bool intersectingOrInside3 = GetSignedDistanceFromPlane(center, planes[3]) < radius;
    
    return intersectingOrInside0 && intersectingOrInside1 && intersectingOrInside2 && intersectingOrInside3;
}

[numthreads(TILE_SIZE, TILE_SIZE, 1)]
void CSMain(uint3 globalID : SV_DispatchThreadID, uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID)
{
    // Convert the 2D value from localID into a single index that can be used in the for loop.
    const uint localIDindex = localID.y * TILE_SIZE + localID.x;

    if (localIDindex == 0)
        LightIndexCounter = 0;

    float3 frustumEquations[4];
    uint pxm = TILE_SIZE * groupID.x;
    uint pym = TILE_SIZE * groupID.y;
    uint pxp = TILE_SIZE * (groupID.x + 1);
    uint pyp = TILE_SIZE * (groupID.y + 1);

    uint2 targetSizeEvenlyDivisibleByTileRes = GetNumberOfTiles() * TILE_SIZE;

    float3 frustum0 = ConvertProjToView(float4(pxm / (float)targetSizeEvenlyDivisibleByTileRes.x * 2.0 - 1.0, (targetSizeEvenlyDivisibleByTileRes.y - pym) / (float)targetSizeEvenlyDivisibleByTileRes.y * 2.0 - 1.0, 1.0, 1.0)).xyz;
    float3 frustum1 = ConvertProjToView(float4(pxp / (float)targetSizeEvenlyDivisibleByTileRes.x * 2.0 - 1.0, (targetSizeEvenlyDivisibleByTileRes.y - pym) / (float)targetSizeEvenlyDivisibleByTileRes.y * 2.0 - 1.0, 1.0, 1.0)).xyz;
    float3 frustum2 = ConvertProjToView(float4(pxp / (float)targetSizeEvenlyDivisibleByTileRes.x * 2.0 - 1.0, (targetSizeEvenlyDivisibleByTileRes.y - pyp) / (float)targetSizeEvenlyDivisibleByTileRes.y * 2.0 - 1.0, 1.0, 1.0)).xyz;
    float3 frustum3 = ConvertProjToView(float4(pxm / (float)targetSizeEvenlyDivisibleByTileRes.x * 2.0 - 1.0, (targetSizeEvenlyDivisibleByTileRes.y - pyp) / (float)targetSizeEvenlyDivisibleByTileRes.y * 2.0 - 1.0, 1.0, 1.0)).xyz;

    frustumEquations[0] = CreatePlaneEquation(frustum0, frustum1);
    frustumEquations[1] = CreatePlaneEquation(frustum1, frustum2);
    frustumEquations[2] = CreatePlaneEquation(frustum2, frustum3);
    frustumEquations[3] = CreatePlaneEquation(frustum3, frustum0);

    GroupMemoryBarrierWithGroupSync();
    
    for (uint i = localIDindex; i < gScene.NumLights; i += NUM_THREADS_PER_TILE)
    {
        float3 center = SceneLights[i].Position;
        const float radius = 2;
        center.xyz = mul(gScene.Camera.Projection, mul(gScene.Camera.View, float4(center, 1.0))).xyz;
        
        if (TestFrustumSides(center, radius, frustumEquations))
        {
            if (-center.z < radius)
            {
                uint destinationIndex = 0;
                InterlockedAdd(LightIndexCounter, 1, destinationIndex);
                LightIndices[destinationIndex] = i;
            }
        }
    }
    
    GroupMemoryBarrierWithGroupSync();
}