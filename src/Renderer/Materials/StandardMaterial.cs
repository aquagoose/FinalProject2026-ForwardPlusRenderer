using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Structs;
using Renderer.Utils;
using SDL3;

namespace Renderer.Materials;

/// <summary>
/// The standard, lit material using the metallic-roughness-occlusion model.
/// </summary>
public sealed class StandardMaterial : Material
{
    private IntPtr _buffer;
    
    /// <summary>
    /// The albedo/base texture.
    /// </summary>
    public Texture Albedo;

    /// <summary>
    /// The normal texture.
    /// </summary>
    public Texture Normal;

    /// <summary>
    /// The metallic texture.
    /// </summary>
    public Texture Metallic;

    /// <summary>
    /// The roughness texture.
    /// </summary>
    public Texture Roughness;

    /// <summary>
    /// The occlusion texture.
    /// </summary>
    public Texture Occlusion;

    /// <summary>
    /// Create a <see cref="StandardMaterial"/> with an albedo texture and a default set of other textures.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this material with.</param>
    /// <param name="albedo">The albedo/base texture.</param>
    public unsafe StandardMaterial(Renderer renderer, Texture albedo, MaterialInfo info = new())
        : base(renderer, in info, "Materials/BaseVertex", "Materials/StandardMaterial", 5)
    {
        Albedo = albedo;
        Normal = renderer.EmptyNormalTexture;
        Metallic = renderer.WhiteTexture;
        Roughness = renderer.WhiteTexture;
        Occlusion = renderer.WhiteTexture;

        IntPtr device = renderer.Device;
        _buffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.GraphicsStorageRead,
            (uint) sizeof(ShaderLight) * 32);

        ShaderLight[] lights =
        [
            new ShaderLight { Type = ShaderLight.LightType.Point, Position = new Vector4(0, 1, 0, 0), Color = new Color(1.0f, 0.0f, 0.0f) },
            new ShaderLight { Type = ShaderLight.LightType.Point, Position = new Vector4(-8, 1, -8, 0), Color = new Color(0.0f, 1.0f, 0.0f) },
            new ShaderLight { Type = ShaderLight.LightType.Point, Position = new Vector4(8, 1, -8, 0), Color = new Color(0.0f, 0.0f, 1.0f) },
            new ShaderLight { Type = ShaderLight.LightType.Point, Position = new Vector4(8, 1, 8, 0), Color = new Color(1.0f, 1.0f, 0.0f) },
            new ShaderLight { Type = ShaderLight.LightType.Point, Position = new Vector4(-8, 1, 8, 0), Color = new Color(0.0f, 1.0f, 1.0f) }
        ];
        
        renderer.UpdateBuffer(_buffer, 0, lights);
    }

    public override void ReleaseAllTexturesAndDispose()
    {
        Occlusion.Dispose();
        Roughness.Dispose();
        Metallic.Dispose();
        Normal.Dispose();
        Albedo.Dispose();
        base.ReleaseAllTexturesAndDispose();
    }

    /// <inheritdoc />
    protected override void PopulateTextureBindings(ref SDL.GPUTextureSamplerBinding[] bindings)
    {
        bindings[0] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Albedo.TextureHandle,
            Sampler = Albedo.Sampler
        };

        bindings[1] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Normal.TextureHandle,
            Sampler = Normal.Sampler
        };

        bindings[2] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Metallic.TextureHandle,
            Sampler = Metallic.Sampler
        };

        bindings[3] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Roughness.TextureHandle,
            Sampler = Roughness.Sampler
        };

        bindings[4] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Occlusion.TextureHandle,
            Sampler = Occlusion.Sampler
        };
    }

    protected internal override unsafe void BindFrameResources(IntPtr pass)
    {
        SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint) Unsafe.AsPointer(ref _buffer), 1);
    }
}