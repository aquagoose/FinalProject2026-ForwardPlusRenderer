using System.Drawing;
using System.Numerics;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Primitives;
using Renderer.Tests.Common;
using Plane = Renderer.Primitives.Plane;
using Rectangle = Renderer.Math.Rectangle;
using Size = Renderer.Math.Size;

namespace Renderer.Tests.BasicLighting;

public class BasicLightingTest() : TestBase("Basic Lighting Test")
{
    private Texture _albedo;
    private Texture _normal;
    private Texture _metallic;
    private Texture _roughness;
    private Texture _occlusion;
    
    private Material _material = null!;
    private Renderable _renderable = null!;
    private float _rotation;
    
    protected override void Load()
    {
        Renderer.BackgroundColor = Color.Black;

        _albedo = new Texture(Renderer, "Content/PBR/metalgrid3_basecolor.png");
        _normal = new Texture(Renderer, "Content/PBR/metalgrid3_normal-dx.png");
        _metallic = new Texture(Renderer, "Content/PBR/metalgrid3_metallic.png");
        _roughness = new Texture(Renderer, "Content/PBR/metalgrid3_roughness.png");
        _occlusion = new Texture(Renderer, "Content/PBR/metalgrid3_AO.png");
        
        _material = new StandardMaterial(Renderer, _albedo)
        {
            Normal = _normal,
            Metallic = _metallic,
            Roughness = _roughness,
            Occlusion = _occlusion
        };

        IPrimitive primitive = new Cube();
        _renderable = new Renderable(Renderer, _material, primitive.Vertices, primitive.Indices);
    }

    protected override void Loop(float dt)
    {
        _rotation += dt;
        if (_rotation >= float.Pi * 2 * 4)
            _rotation -= float.Pi * 2 * 4;

        float rotationOffset = 0;
        for (int x = -10; x < 11; x++)
        for (int z = -10; z < 11; z++)
        {
            Renderer.Draw(_renderable,
                Matrix4x4.CreateFromYawPitchRoll(_rotation + rotationOffset, (_rotation * 0.5f) + rotationOffset, (_rotation * 0.25f) + rotationOffset) *
                Matrix4x4.CreateTranslation(x, 0, z));

            rotationOffset += 0.05f;
        }
        
        Camera camera = Camera.Perspective(new Vector3(-3, 4, 3), Quaternion.CreateFromYawPitchRoll(-1, -1, 0), float.DegreesToRadians(45),
            new Rectangle(Offset.Zero, Size), 0.1f, 100f);
        Renderer.AddCamera(in camera);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        _occlusion.Dispose();
        _roughness.Dispose();
        _metallic.Dispose();
        _normal.Dispose();
        _albedo.Dispose();
        base.Dispose();
    }
}