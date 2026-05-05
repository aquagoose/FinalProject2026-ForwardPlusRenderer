using System.Numerics;
using Renderer.Math;
using Renderer.Structs;
using SDL3;

namespace Renderer.Renderers;

internal interface IRenderer : IDisposable
{
    public Color BackgroundColor { get; set; }

    public void ClearDrawQueues();
    
    public void AddLight(ref readonly ShaderLight light);
    
    public void AddOpaqueRenderable(Renderable renderable, in Matrix4x4 world);

    public void RenderCamera(IntPtr cb, IntPtr colorTexture, IntPtr depthTexture, Camera camera, bool clear);

    public void Resize(Size size);
}