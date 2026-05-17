#ifndef FORWARDPLUS_COMMON
#define FORWARDPLUS_COMMON

#include "../Common.hlsli"

#define TILE_SIZE 16
#define MAX_LIGHTS_PER_TILE 1024

#define LIGHT_BUFFER_END_OF_ARRAY 0x7FFFFFF

// Calculate the number of tiles. This adds one extra tile if the screen size does not cleanly divide by TILE_SIZE.
uint2 GetNumberOfTiles(const uint2 targetSize)
{
    const uint x = (uint) ((targetSize.x + TILE_SIZE - 1) / (float) TILE_SIZE);
    const uint y = (uint) ((targetSize.y + TILE_SIZE - 1) / (float) TILE_SIZE);
    return uint2(x, y);
}

#endif