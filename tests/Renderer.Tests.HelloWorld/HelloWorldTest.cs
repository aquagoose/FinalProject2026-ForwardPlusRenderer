using System.Numerics;
using Renderer.Materials;
using Renderer.Tests.Common;

namespace Renderer.Tests.HelloWorld;

public class HelloWorldTest() : TestBase("Hello World")
{
    private Material _material = null!;
    private Renderable _renderable = null!;
    
    protected override void Load()
    {
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

        _material = new UnlitMaterial(Renderer);
        _renderable = new Renderable(Renderer, _material, vertices, indices);
    }

    protected override void Loop(float dt)
    {
        Matrix4x4 world = Matrix4x4.Identity;
        Renderer.Draw(_renderable, world);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        base.Dispose();
    }
}