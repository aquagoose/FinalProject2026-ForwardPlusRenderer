RWTexture2D<float4> Texture : register(u0, space1);

[numthreads(8, 8, 1)]
void CSMain(uint3 dispatchID : SV_DispatchThreadID)
{
    float2 size = 0;
    Texture.GetDimensions(size.x, size.y);
    Texture[int2(dispatchID.xy)] = float4(dispatchID.xy / size, 0.0, 1.0);
}