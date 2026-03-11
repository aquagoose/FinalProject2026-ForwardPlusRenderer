using Renderer.Utils;
using SDL3;

namespace Renderer;

public class Renderable : IDisposable
{
    private readonly Renderer _renderer;

    public readonly IntPtr VertexBuffer;
    public readonly IntPtr IndexBuffer;

    public unsafe Renderable(Renderer renderer, in ReadOnlySpan<Vertex> vertices, in ReadOnlySpan<uint> indices)
    {
        _renderer = renderer;
        IntPtr device = _renderer.Device;

        VertexBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.Vertex,
            (uint) (vertices.Length * sizeof(Vertex)));
        _renderer.UpdateBuffer(VertexBuffer, 0, in vertices);
        
        IndexBuffer =
            SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.Index, (uint) (indices.Length * sizeof(uint)));
        _renderer.UpdateBuffer(IndexBuffer, 0, in indices);
    }
    
    public void Dispose()
    {
        IntPtr device = _renderer.Device;
        SDL.ReleaseGPUBuffer(device, IndexBuffer);
        SDL.ReleaseGPUBuffer(device, VertexBuffer);
    }
}