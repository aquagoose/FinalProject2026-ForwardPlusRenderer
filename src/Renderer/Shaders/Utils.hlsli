// Defines various helper macros for Vertex and Pixel shader uniforms/textures
// as SDL_GPU requires each type to be in the correct "space", so these macros
// ensure that everything will always be in the right "space".

#pragma once

// Define a 2D texture sampler (Texture + SamplerState) to be sampled in a pixel shader.
#define SAMPLER2D_PS(Name, Location) Texture2D Name : register(t##Location, space2);\
    SamplerState Name##Sampler : register(s##Location, space2);

// Sample from a texture.
#define SAMPLE(Name, TexCoord) Name.Sample(Name##Sampler, TexCoord)

#define UNIFORM_PS(Name, Type, Location) cbuffer Name##Buffer : register(b##Location, space3) { Type Name; }