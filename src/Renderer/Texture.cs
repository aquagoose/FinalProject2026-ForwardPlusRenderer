using Renderer.Utils;
using SDL3;

namespace Renderer;

public sealed class Texture : IDisposable
{
    private readonly IntPtr _device;

    internal readonly IntPtr TextureHandle;
    
    // TODO: Sampler struct, Renderer.GetSampler (like Sprout)
    internal readonly IntPtr Sampler;

    public Texture(Renderer renderer, byte[] data, Size size, PixelFormat format)
    {
        _device = renderer.Device;

        (SDL.GPUTextureFormat sdlFormat, uint bytesPerPixel) = format switch
        {
            PixelFormat.RGBA8 => (SDL.GPUTextureFormat.R8G8B8A8Unorm, 4u),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        TextureHandle = SDLUtils.CreateTexture(_device, SDL.GPUTextureType.TextureType2D, sdlFormat, size.Width,
            size.Height, SDL.GPUTextureUsageFlags.Sampler);
        
        renderer.UpdateTexture(TextureHandle, 0, 0, size.Width, size.Height, bytesPerPixel, data);
        
        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.Repeat,
            AddressModeV = SDL.GPUSamplerAddressMode.Repeat,
            MinLod = 0,
            MaxLod = float.MaxValue
        };

        Sampler = SDL.CreateGPUSampler(_device, in samplerInfo).Check("Create sampler");
    }

    public Texture(Renderer renderer, Bitmap bitmap) : this(renderer, bitmap.Data, bitmap.Size, bitmap.Format) { }

    public Texture(Renderer renderer, string path) : this(renderer, new Bitmap(path)) { }

    public void Dispose()
    {
        SDL.ReleaseGPUSampler(_device, Sampler);
        SDL.ReleaseGPUTexture(_device, TextureHandle);
    }
}