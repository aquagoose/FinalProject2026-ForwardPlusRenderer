using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Structs;
using Renderer.Utils;
using SDL3;

namespace Renderer.Renderers;

internal class ForwardPlusRenderer : ISceneRenderer
{
    private readonly Renderer _renderer;
    private readonly List<(Renderable renderable, Matrix4x4 world)> _opaques;

    private ShaderLight[] _lights;
    private uint _numLights;
    private IntPtr _lightBuffer;

    public Color BackgroundColor { get; set; }
    
    public unsafe ForwardPlusRenderer(Renderer renderer)
    {
        _renderer = renderer;
        _opaques = [];

        IntPtr device = _renderer.Device;
        
        const uint defaultMaxLights = 1024;
        _lights = new ShaderLight[defaultMaxLights];
        _lightBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.GraphicsStorageRead,
            defaultMaxLights * (uint) sizeof(ShaderLight));
    }

    public void ClearDrawQueues()
    {
        _opaques.Clear();
        _numLights = 0;
    }

    public void AddOpaqueRenderable(Renderable renderable, in Matrix4x4 world)
    {
        _opaques.Add((renderable, world));
    }

    public unsafe void AddLight(ref readonly ShaderLight light)
    {
        if (_numLights + 1 >= _lights.Length)
        {
            IntPtr device = _renderer.Device;
            
            Array.Resize(ref _lights, _lights.Length << 1);
            SDL.ReleaseGPUBuffer(device, _lightBuffer);
            _lightBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.GraphicsStorageRead,
                (uint) (_lights.Length * sizeof(ShaderLight)));
        }

        _lights[_numLights++] = light;
    }

    public unsafe void RenderCamera(IntPtr cb, IntPtr colorTexture, IntPtr depthTexture, Camera camera, bool clear)
    {
        // TODO: UpdateBuffer with overload that accepts a command buffer.
        _renderer.UpdateBuffer(_lightBuffer, 0, _lights);
        
        // TODO: Better way of doing this?
        Matrix4x4.Invert(camera.View, out Matrix4x4 inverseView);
        SceneData sceneData = new()
        {
            Camera = new ShaderCamera(camera.Projection, camera.View, new Vector4(inverseView.Translation, 0)),
            NumLights = _numLights
        };
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        SDL.PushGPUFragmentUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        
        SDL.GPUColorTargetInfo colorTarget = new()
        {
            Texture = colorTexture,
            ClearColor = new SDL.FColor(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, BackgroundColor.A),
            LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };
        
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
        
        SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint) Unsafe.AsPointer(ref _lightBuffer), 1);
        
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