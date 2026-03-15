using System.Drawing;
using System.Numerics;
using Renderer.Materials;
using Renderer.Tests.Common;

namespace Renderer.Tests.HelloWorld;

public class HelloWorldTest() : TestBase("Hello World")
{
    private Material _material = null!;
    private Renderable _renderable = null!;

    private float _rotation;
    
    protected override void Load()
    {
        Renderer.BackgroundColor = Color.CornflowerBlue;
        
        /*
         *   1 ------- 2
         *   |   \     |
         *   |    \    |
         *   |     \   |
         *   0 ------- 3
         */
        ReadOnlySpan<Vertex> vertices =
        [
            new Vertex(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 0), new Color(1.0f, 0.0f, 0.0f)), // 0
            new Vertex(new Vector3(-0.5f,  0.5f, 0), new Vector2(0, 1), new Color(0.0f, 1.0f, 0.0f)), // 1
            new Vertex(new Vector3( 0.5f,  0.5f, 0), new Vector2(1, 1), new Color(0.0f, 0.0f, 1.0f)), // 2
            new Vertex(new Vector3( 0.5f, -0.5f, 0), new Vector2(1, 0), new Color(0.0f, 0.0f, 0.0f))  // 3
        ];

        ReadOnlySpan<uint> indices =
        [
            0, 1, 3,
            1, 2, 3
        ];

        _material = new UnlitMaterial(Renderer, Renderer.WhiteTexture);
        _renderable = new Renderable(Renderer, _material, vertices, indices);
    }

    protected override void Loop(float dt)
    {
        _rotation = (_rotation + dt) % (float.Pi * 2);
        
        Matrix4x4 world = Matrix4x4.CreateRotationY(_rotation);
        Renderer.Draw(_renderable, world);

        Size size = Size;
        Camera camera = Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, (int) size.Width, (int) size.Height), 0.1f, 100f);
        Renderer.AddCamera(camera);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        base.Dispose();
    }
}