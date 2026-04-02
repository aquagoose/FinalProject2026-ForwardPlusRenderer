using System.Numerics;
using Renderer.Materials;
using Renderer.Renderers.Structs;
using Renderer.Utils;
using SDL3;

namespace Renderer.Renderers;

internal class ForwardPlusRenderer : ISceneRenderer
{
    private readonly IntPtr _device;
    private readonly List<(Renderable renderable, Matrix4x4 world)> _opaques;

    public Color BackgroundColor { get; set; }
    
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

    public unsafe void RenderCamera(IntPtr cb, IntPtr colorTexture, IntPtr depthTexture, Camera camera, bool clear)
    {
        // TODO: Better way of doing this?
        Matrix4x4.Invert(camera.View, out Matrix4x4 inverseView);
        ShaderCamera shaderCamera = new ShaderCamera(camera.Projection, camera.View, new Vector4(inverseView.Translation, 0));
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&shaderCamera), (uint) sizeof(ShaderCamera));
        SDL.PushGPUFragmentUniformData(cb, 0, new IntPtr(&shaderCamera), (uint) sizeof(ShaderCamera));
        
        SDL.GPUColorTargetInfo colorTarget = new()
        {
            Texture = colorTexture,
            ClearColor = new SDL.FColor(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, BackgroundColor.A),
            LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };

        // TODO: Find out what is causing the validation error?
        SDL.GPUDepthStencilTargetInfo depthTarget = new()
        {
            Texture = depthTexture,
            LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store,
            ClearDepth = 1.0f
        };

        IntPtr pass = SDL.BeginGPURenderPass(cb, new IntPtr(&colorTarget), 1, in depthTarget).Check("Begin render pass");
        SDL.SetGPUViewport(pass, new SDL.GPUViewport
        {
            X = camera.Viewport.X,
            Y = camera.Viewport.Y,
            W = camera.Viewport.Width,
            H = camera.Viewport.Height,
            MinDepth = 0,
            MaxDepth = 1
        });
        
        foreach ((Renderable renderable, Matrix4x4 world) in _opaques)
        {
            Material material = renderable.Material;

            SDL.PushGPUVertexUniformData(cb, 1, new IntPtr(&world), (uint) sizeof(Matrix4x4));

            Matrix4x4.Invert(world, out Matrix4x4 normalMatrix);
            normalMatrix = Matrix4x4.Transpose(normalMatrix);
            SDL.PushGPUVertexUniformData(cb, 2, new IntPtr(&normalMatrix), (uint) sizeof(Matrix4x4));
            
            SDL.BindGPUGraphicsPipeline(pass, material.Pipeline);

            SDL.GPUTextureSamplerBinding[] textureBindings = material.TextureBindings;
            SDL.BindGPUFragmentSamplers(pass, 0, textureBindings, (uint) textureBindings.Length);

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

    public void Resize(Size size)
    {
        
    }

    public void Dispose()
    {
        
    }
}