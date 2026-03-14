using System.Numerics;

namespace Renderer.Primitives;

public class Plane : IPrimitive
{
    public Vertex[] Vertices { get; }
    
    public uint[] Indices { get; }

    public Plane()
    {
        /*
         *   1 ------- 2
         *   |   \     |
         *   |    \    |
         *   |     \   |
         *   0 ------- 3
         */
        Vertices =
        [
            new Vertex(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1), new Color(1.0f, 1.0f, 1.0f)), // 0
            new Vertex(new Vector3(-0.5f,  0.5f, 0), new Vector2(0, 0), new Color(1.0f, 1.0f, 1.0f)), // 1
            new Vertex(new Vector3( 0.5f,  0.5f, 0), new Vector2(1, 0), new Color(1.0f, 1.0f, 1.0f)), // 2
            new Vertex(new Vector3( 0.5f, -0.5f, 0), new Vector2(1, 1), new Color(1.0f, 1.0f, 1.0f))  // 3
        ];

        Indices =
        [
            0, 1, 3,
            1, 2, 3
        ];
    }
}