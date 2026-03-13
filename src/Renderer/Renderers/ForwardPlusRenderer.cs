using System.Numerics;
using Renderer.Materials;
using Renderer.Utils;
using SDL3;

namespace Renderer.Renderers;

internal class ForwardPlusRenderer : IRenderer
{
    private readonly IntPtr _device;
    private readonly List<(Renderable renderable, Matrix4x4 world)> _opaques;

    public ForwardPlusRenderer(IntPtr device)
    {
        _device = device;
        _opaques = [];
    }

    public void ClearDrawQueues()
    {
        _opaques.Clear();
    }

    public void AddOpaqueRenderable(Renderable renderable, in Matrix4x4 world)
    {
        _opaques.Add((renderable, world));
    }

    public unsafe void RenderCamera(IntPtr cb, IntPtr outputTarget, SDL.GPUTextureFormat outputFormat, Camera camera)
    {
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&camera), (uint) sizeof(Camera));
        
        SDL.GPUColorTargetInfo targetInfo = new()
        {
            Texture = outputTarget,
            ClearColor = new SDL.FColor(1.0f, 0.5f, 0.25f, 1.0f),
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store
        };

        IntPtr pass = SDL.BeginGPURenderPass(cb, new IntPtr(&targetInfo), 1, IntPtr.Zero).Check("Begin render pass");
        
        foreach ((Renderable renderable, Matrix4x4 world) in _opaques)
        {
            Material material = renderable.Material;

            SDL.PushGPUVertexUniformData(cb, 1, new IntPtr(&world), (uint) sizeof(Matrix4x4));
            
            SDL.BindGPUGraphicsPipeline(pass, material.Pipeline);

            SDL.GPUBufferBinding vertexBufferBinding = new()
            {
                Buffer = renderable.VertexBuffer,
                Offset = 0
            };
            SDL.BindGPUVertexBuffers(pass, 0, new IntPtr(&vertexBufferBinding), 1);

            SDL.GPUBufferBinding indexBufferBinding = new()
            {
                Buffer = renderable.IndexBuffer,
                Offset = 0
            };
            SDL.BindGPUIndexBuffer(pass, in indexBufferBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);

            SDL.DrawGPUIndexedPrimitives(pass, renderable.NumDraws, 1, 0, 0, 0);
        }
        
        SDL.EndGPURenderPass(pass);
    }
    
    public void Dispose()
    {
        
    }
}