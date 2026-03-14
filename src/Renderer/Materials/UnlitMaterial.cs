using SDL3;

namespace Renderer.Materials;

public class UnlitMaterial : Material
{
    public Texture Texture;

    public UnlitMaterial(Renderer renderer, Texture texture)
        : base(renderer, "Materials/BaseVertex", "Materials/UnlitMaterial", 1)
    {
        Texture = texture;
    }

    protected internal override void PopulateTextureBindings(ref SDL.GPUTextureSamplerBinding[] bindings)
    {
        bindings[0] = new SDL.GPUTextureSamplerBinding
        {
            Texture = Texture.TextureHandle,
            Sampler = Texture.Sampler
        };
    }
}