#define SAMPLER2D_FS(Name, Location) Texture2D Name : register(t##Location, space2);\
    SamplerState Name##Sampler : register(s##Location, space2);

#define SAMPLE(Name, TexCoord) Name.Sample(Name##Sampler, TexCoord)