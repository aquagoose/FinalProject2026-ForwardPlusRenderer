RWTexture2D<float4> Texture : register(u0, space1);

[numthreads(8, 8, 1)]
void CSMain(uint3 dispatchID : SV_DispatchThreadID)
{
    Texture[int2(dispatchID.xy)] = float4(1.0f, 0.5f, 0.25f, 1.0f);
}