using System.Numerics;
using Renderer.Materials;
using Renderer.Primitives;
using Renderer.Tests.Common;
using Plane = Renderer.Primitives.Plane;

namespace Renderer.Tests.HelloTexture;

public class HelloTextureTest() : TestBase("Hello Texture")
{
    private Texture _texture = null!;
    private Material _material = null!;
    private Renderable _renderable = null!;

    private float _rotation;

    protected override void Load()
    {
        _texture = new Texture(Renderer, "Content/bagel.png");
        _material = new UnlitMaterial(Renderer, _texture);
        
        IPrimitive primitive = new Plane();
        _renderable = new Renderable(Renderer, _material, primitive.Vertices, primitive.Indices);
    }

    protected override void Loop(float dt)
    {
        _rotation = (_rotation + dt) % (float.Pi * 2);
        
        Renderer.Draw(_renderable, Matrix4x4.CreateRotationY(_rotation));
        
        Camera camera = Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            1280 / 720f, 0.1f, 100f);
        Renderer.AddCamera(in camera);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        _texture.Dispose();
        base.Dispose();
    }
}