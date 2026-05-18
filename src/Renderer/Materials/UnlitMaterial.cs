using SDL3;

namespace Renderer.Materials;

/// <summary>
/// A textured <see cref="Material"/> with no lighting applied whatsoever.
/// </summary>
public sealed class UnlitMaterial : Material
{
    /// <summary>
    /// The <see cref="Texture"/> of the material.
    /// </summary>
    public Texture Texture;

    /// <summary>
    /// Create an unlit material.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this material with.</param>
    /// <param name="texture">The <see cref="Texture"/> of the material.</param>
    public UnlitMaterial(Renderer renderer, Texture texture, MaterialInfo info = new(), bool useScreenDoor = false)
        : base(renderer, in info, "Materials/BaseVertex", useScreenDoor ? "Materials/UnlitMaterialScreenDoor" : "Materials/UnlitMaterial", 1)
    {
        Texture = texture;
    }

    /// <inheritdoc />
    public override void ReleaseAllTexturesAndDispose()
    {
        Texture.Dispose();
        base.ReleaseAllTexturesAndDispose();
    }

    /// <inheritdoc />
    protected override void PopulateTextureBindings(ref SDL.GPUTextureSamplerBinding[] bindings)
    {
        bindings[0] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Texture.Handle,
            Sampler = Texture.Sampler
        };
    }
}