using Renderer.Materials;
using Renderer.Utils;
using SDL3;

namespace Renderer;

/// <summary>
/// A drawable 3D object.
/// </summary>
public class Renderable : IDisposable
{
    private readonly Renderer _renderer;

    internal readonly IntPtr VertexBuffer;
    internal readonly IntPtr IndexBuffer;

    internal readonly uint NumDraws;

    /// <summary>
    /// The <see cref="Materials.Material"/> that the object is associated with.
    /// </summary>
    public Material Material;

    /// <summary>
    /// Create a <see cref="Renderable"/> from a material, vertices, and indices.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this renderable with.</param>
    /// <param name="material">The <see cref="Materials.Material"/> assigned to this renderable.</param>
    /// <param name="vertices">The vertices to use.</param>
    /// <param name="indices">The indices to use.</param>
    public unsafe Renderable(Renderer renderer, Material material, in ReadOnlySpan<Vertex> vertices,
        in ReadOnlySpan<uint> indices)
    {
        _renderer = renderer;
        Material = material;
        IntPtr device = _renderer.Device;

        VertexBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.Vertex,
            (uint) (vertices.Length * sizeof(Vertex)));
        _renderer.UpdateBuffer(VertexBuffer, 0, in vertices);
        
        IndexBuffer =
            SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.Index, (uint) (indices.Length * sizeof(uint)));
        _renderer.UpdateBuffer(IndexBuffer, 0, in indices);

        NumDraws = (uint) indices.Length;
    }
    
    /// <summary>
    /// Dispose of this <see cref="Renderable"/>.
    /// </summary>
    public void Dispose()
    {
        IntPtr device = _renderer.Device;
        SDL.ReleaseGPUBuffer(device, IndexBuffer);
        SDL.ReleaseGPUBuffer(device, VertexBuffer);
    }
}