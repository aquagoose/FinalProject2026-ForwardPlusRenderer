namespace Renderer.Primitives;

/// <summary>
/// The base interface for primitives, which are simple objects, useful for many applications.
/// </summary>
public interface IPrimitive
{
    /// <summary>
    /// The primitive's vertices.
    /// </summary>
    public Vertex[] Vertices { get; }
    
    /// <summary>
    /// The primitive's indices.
    /// </summary>
    public uint[] Indices { get; }
}