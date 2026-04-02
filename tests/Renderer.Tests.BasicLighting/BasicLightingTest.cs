using System.Drawing;
using System.Numerics;
using Renderer.Materials;
using Renderer.Primitives;
using Renderer.Tests.Common;
using Plane = Renderer.Primitives.Plane;

namespace Renderer.Tests.BasicLighting;

public class BasicLightingTest() : TestBase("Basic Lighting Test")
{
    private Material _material = null!;
    private Renderable _renderable = null!;
    private float _rotation;
    
    protected override void Load()
    {
        Renderer.BackgroundColor = new Color(0.1f, 0.1f, 0.1f);
        
        _material = new StandardMaterial(Renderer, new Texture(Renderer, "Content/PBR/metalgrid4_basecolor.png"))
        {
            Normal = new Texture(Renderer, "Content/PBR/metalgrid4_normal-ogl.png"),
            Metallic = new Texture(Renderer, "Content/PBR/metalgrid4_metallic.png"),
            Roughness = new Texture(Renderer, "Content/PBR/metalgrid4_roughness.png"),
            Occlusion = new Texture(Renderer, "Content/PBR/metalgrid4_AO.png")
        };

        IPrimitive primitive = new Cube();
        _renderable = new Renderable(Renderer, _material, primitive.Vertices, primitive.Indices);
    }

    protected override void Loop(float dt)
    {
        _rotation += dt;
        if (_rotation >= float.Pi * 2)
            _rotation -= float.Pi * 2;

        Renderer.Draw(_renderable, Matrix4x4.CreateFromYawPitchRoll(_rotation, _rotation * 0.5f, _rotation * 0.25f));
        
        Size size = Size;
        Camera camera = Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, (int) size.Width, (int) size.Height), 0.1f, 100f);
        Renderer.AddCamera(in camera);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        base.Dispose();
    }
}