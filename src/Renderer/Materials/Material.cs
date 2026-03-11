using SDL3;

namespace Renderer.Materials;

/// <summary>
/// A material describes how an object is rendered.
/// </summary>
public abstract class Material : IDisposable
{
    internal readonly IntPtr Pipeline;

    protected Material(Renderer renderer, string vertexShader, string pixelShader)
    {
        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            
        }
    }
    
    public virtual void Dispose()
    {
        
    }
}