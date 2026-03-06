namespace Renderer.Renderers;

internal class ForwardPlusRenderer : IRenderer
{
    private readonly IntPtr _device;
    
    public ForwardPlusRenderer(IntPtr device)
    {
        _device = device;
    }
    
    public void Dispose()
    {
        
    }
}