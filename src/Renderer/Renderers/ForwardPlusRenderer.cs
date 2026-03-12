using SDL3;

namespace Renderer.Renderers;

internal class ForwardPlusRenderer : IRenderer
{
    private readonly IntPtr _device;

    public SDL.GPUTextureFormat MainTargetFormat => SDL.GPUTextureFormat.R10G10B10A2Unorm;
    
    public ForwardPlusRenderer(IntPtr device)
    {
        _device = device;
    }
    
    public void Dispose()
    {
        
    }
}