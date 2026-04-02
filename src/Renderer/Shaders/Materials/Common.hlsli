#pragma once

#include "../Common.hlsli"

struct VertexOutput
{
    float4 Position: SV_Position;
    float3 WorldPos: POSITION0;
    float2 TexCoord: TEXCOORD0;
    float4 Color:    COLOR0;
    float3 Normal:   NORMAL0;
};