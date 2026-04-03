#ifndef COMMON_H
#define COMMON_H

#include "Utils.hlsli"

struct Camera
{
    float4x4 Projection;
    float4x4 View;
    float4 Position;
};

#endif