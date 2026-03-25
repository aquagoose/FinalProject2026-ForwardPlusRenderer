using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer;

/// <summary>
/// A standard vertex used for all 3D renderers and materials. This ensures compatability between them.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    /// <summary>
    /// The vertex's position.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The vertex's texture coordinate.
    /// </summary>
    public Vector2 TexCoord;

    /// <summary>
    /// The vertex color.
    /// </summary>
    public Color Color;

    /// <summary>
    /// The normal vector.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// Create a <see cref="Vertex"/>.
    /// </summary>
    /// <param name="position">The vertex's position.</param>
    /// <param name="texCoord">The vertex's texture coordinate.</param>
    /// <param name="color">The vertex color.</param>
    /// <param name="normal">The normal vector.</param>
    public Vertex(Vector3 position, Vector2 texCoord, Color color, Vector3 normal)
    {
        Position = position;
        TexCoord = texCoord;
        Color = color;
        Normal = normal;
    }
}