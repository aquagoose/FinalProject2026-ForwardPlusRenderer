#pragma once

#include "Utils.hlsli"

struct Camera
{
    float4x4 Projection;
    float4x4 View;
    float4 Position;
};