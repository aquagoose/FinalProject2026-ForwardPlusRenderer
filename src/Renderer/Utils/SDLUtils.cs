using SDL3;

namespace Renderer.Utils;

internal static class SDLUtils
{
    public static IntPtr Check(this IntPtr ptr, string operation)
    {
        if (ptr == IntPtr.Zero)
            throw new Exception($"SDL operation '{operation}' failed: {SDL.GetError()}");

        return ptr;
    }

    public static void Check(this bool b, string operation)
    {
        if (!b)
            throw new Exception($"SDL operation '{operation}' failed: {SDL.GetError()}");
    }

    public static uint CalculateMipLevels(uint width, uint height)
    {
        // https://docs.vulkan.org/tutorial/latest/09_Generating_Mipmaps.html#_image_creation
        return (uint) double.Floor(double.Log2(double.Max(width, height))) + 1;
    }

    public static IntPtr CreateBuffer(IntPtr device, SDL.GPUBufferUsageFlags usage, uint size)
    {
        SDL.GPUBufferCreateInfo bufferInfo = new()
        {
            Usage = usage,
            Size = size
        };

        return SDL.CreateGPUBuffer(device, in bufferInfo).Check("Create buffer");
    }

    public static IntPtr CreateTransferBuffer(IntPtr device, SDL.GPUTransferBufferUsage usage, uint size)
    {
        SDL.GPUTransferBufferCreateInfo bufferInfo = new()
        {
            Usage = usage,
            Size = size
        };

        return SDL.CreateGPUTransferBuffer(device, in bufferInfo).Check("Create transfer buffer");
    }

    public static IntPtr CreateTexture(IntPtr device, SDL.GPUTextureType type, SDL.GPUTextureFormat format, uint width,
        uint height, uint mipLevels, SDL.GPUTextureUsageFlags usage)
    {
        SDL.GPUTextureCreateInfo textureInfo = new()
        {
            Type = type,
            Format = format,
            Width = width,
            Height = height,
            Usage = usage,
            LayerCountOrDepth = 1,
            NumLevels = mipLevels,
            SampleCount = SDL.GPUSampleCount.SampleCount1
        };

        return SDL.CreateGPUTexture(device, in textureInfo).Check("Create texture");
    }
}