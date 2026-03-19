using System.Numerics;

namespace Renderer.Renderers;

internal interface ISceneRenderer : IRenderer
{
    public void ClearDrawQueues();
    
    public void AddOpaqueRenderable(Renderable renderable, in Matrix4x4 world);
}