using Renderer.Math;
using Renderer.Utils;
using SDL3;

namespace Renderer;

/// <summary>
/// A Texture is an image that can be used for rendering.
/// </summary>
public sealed class Texture : IDisposable
{
    private readonly IntPtr _device;
    private readonly uint _mipLevels;

    public readonly IntPtr Handle;

    public readonly Size Size;
    
    // TODO: Sampler struct, Renderer.GetSampler (like Sprout)
    internal readonly IntPtr Sampler;
    
    /// <summary>
    /// Create an empty <see cref="Texture"/>.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this texture with.</param>
    /// <param name="size">The size in pixels.</param>
    /// <param name="format">The <see cref="PixelFormat"/> to use.</param>
    public Texture(Renderer renderer, Size size, PixelFormat format)
    {
        _device = renderer.Device;
        Size = size;

        SDL.GPUTextureFormat sdlFormat = format switch
        {
            PixelFormat.RGBA8 => SDL.GPUTextureFormat.R8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        _mipLevels = SDLUtils.CalculateMipLevels(size.Width, size.Height);

        Handle = SDLUtils.CreateTexture(_device, SDL.GPUTextureType.TextureType2D, sdlFormat, size.Width,
            size.Height, _mipLevels, 1, SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget);
        
        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
            MinLod = 0,
            MaxLod = float.MaxValue,
            EnableAnisotropy = true,
            MaxAnisotropy = 16
        };

        Sampler = SDL.CreateGPUSampler(_device, in samplerInfo).Check("Create sampler");
    }
    
    /// <summary>
    /// Create a <see cref="Texture"/> from an array of pixels.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this texture with.</param>
    /// <param name="data">The initial data to give the texture, if any.</param>
    /// <param name="size">The size in pixels.</param>
    /// <param name="format">The <see cref="PixelFormat"/> to use.</param>
    public Texture(Renderer renderer, ReadOnlySpan<byte> data, Size size, PixelFormat format) : this(renderer, size, format)
    {
        uint bytesPerPixel = format.BytesPerPixel;
        renderer.UpdateTexture(Handle, 0, 0, size.Width, size.Height, bytesPerPixel, data);
        
        // Can only generate mipmaps if there is more than one level, as SDL throws an assertion error otherwise.
        if (_mipLevels > 1)
            renderer.GenerateMipmapsQueue.Enqueue(Handle);
    }

    /// <summary>
    /// Create a <see cref="Texture"/> from a <see cref="Bitmap"/>.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this texture with.</param>
    /// <param name="bitmap">The <see cref="Bitmap"/> containing the pixel data.</param>
    public Texture(Renderer renderer, Bitmap bitmap) : this(renderer, bitmap.Data, bitmap.Size, bitmap.Format) { }

    /// <summary>
    /// Create a <see cref="Texture"/> from a file path.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this texture with.</param>
    /// <param name="path">The path to the image file.</param>
    public Texture(Renderer renderer, string path) : this(renderer, new Bitmap(path)) { }

    /// <summary>
    /// Create a <see cref="Texture"/> from a raw SDL_GPU texture handle.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this texture with.</param>
    /// <param name="handle">The raw texture handle.</param>
    public Texture(Renderer renderer, IntPtr handle)
    {
        _device = renderer.Device;
        Handle = handle;
        
        // TODO: CreateSampler function? Probably should allow for custom samplers.
        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
            MinLod = 0,
            MaxLod = float.MaxValue,
            EnableAnisotropy = true,
            MaxAnisotropy = 16
        };

        Sampler = SDL.CreateGPUSampler(_device, in samplerInfo).Check("Create sampler");
    }

    /// <summary>
    /// Dispose of this <see cref="Texture"/>.
    /// </summary>
    public void Dispose()
    {
        SDL.ReleaseGPUSampler(_device, Sampler);
        SDL.ReleaseGPUTexture(_device, Handle);
    }
}