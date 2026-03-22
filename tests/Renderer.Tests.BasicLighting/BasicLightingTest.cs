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
    
    protected override void Load()
    {
        _material = new StandardMaterial(Renderer, new Texture(Renderer, "Content/DEBUG.png"));

        IPrimitive primitive = new Plane();
        _renderable = new Renderable(Renderer, _material, primitive.Vertices, primitive.Indices);
    }

    protected override void Loop(float dt)
    {
        Renderer.Draw(_renderable, Matrix4x4.Identity);
        
        Size size = Size;
        Camera camera = Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, (int) size.Width, (int) size.Height), 0.1f, 100f);
        Renderer.AddCamera(in camera);
    }
}