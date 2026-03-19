using SDL3;

namespace Renderer.Materials;

/// <summary>
/// The standard, lit material using the metallic-roughness-occlusion model.
/// </summary>
public class StandardMaterial : Material
{
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
    /// 
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="albedo"></param>
    public StandardMaterial(Renderer renderer, Texture albedo)
        : base(renderer, "Materials/BaseVertex", "Materials/StandardMaterial", 5)
    {
        Albedo = albedo;
        Normal = renderer.EmptyNormalTexture;
        Metallic = renderer.WhiteTexture;
        Roughness = renderer.WhiteTexture;
        Occlusion = renderer.WhiteTexture;
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
}