using SDL3;

namespace Renderer.Renderers;

internal interface IRenderer : IDisposable
{
    public SDL.GPUTextureFormat MainTargetFormat { get; }
}