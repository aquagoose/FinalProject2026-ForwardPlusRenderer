using System.Drawing;
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
        Renderer.BackgroundColor = Color.CornflowerBlue;
        
        _texture = new Texture(Renderer, "Content/bagel.png");
        _material = new UnlitMaterial(Renderer, _texture);
        
        IPrimitive primitive = new Plane();
        _renderable = new Renderable(Renderer, _material, primitive.Vertices, primitive.Indices);
    }

    protected override void Loop(float dt)
    {
        _rotation = (_rotation + dt) % (float.Pi * 2);
        
        Renderer.Draw(_renderable, Matrix4x4.CreateRotationY(_rotation));

        Camera camera1 = Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, 1280 / 2, 720), 0.1f, 100f);
        Renderer.AddCamera(in camera1);

        // Multiple cameras
        Camera camera2 = Camera.Perspective(new Vector3(0, -2, 2), Quaternion.CreateFromYawPitchRoll(0, 0.8f, 0), float.DegreesToRadians(45),
            new Rectangle(1280 / 2, 0, 1280 / 2, 720), 0.1f, 100f);
        Renderer.AddCamera(in camera2);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        _texture.Dispose();
        base.Dispose();
    }
}