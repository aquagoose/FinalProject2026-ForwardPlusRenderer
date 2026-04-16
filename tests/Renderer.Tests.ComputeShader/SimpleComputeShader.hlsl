RWTexture2D<float> Texture : register(u0);

[numthreads(1, 1, 1)]
void CSMain(uint3 dispatchID : SV_DispatchThreadID)
{
    Texture[dispatchID.xy] = float4(1.0, 0.5, 0.25, 1.0);
}