namespace Renderer.Primitives;

public interface IPrimitive
{
    public Vertex[] Vertices { get; }
    
    public uint[] Indices { get; }
}