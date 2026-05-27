#ifndef COMMON_H
#define COMMON_H

#include "Utils.hlsli"

struct Camera
{
    float4x4 Projection;
    float4x4 InverseProjection;
    float4x4 View;
    float4 Position;
};

struct Scene
{
    Camera Camera;
    uint2 TargetSize;
    uint NumLights;
    bool UseLightIndices;
};

struct Object
{
    float4x4 WorldMatrix;
    float4x4 NormalMatrix;
};

#endif