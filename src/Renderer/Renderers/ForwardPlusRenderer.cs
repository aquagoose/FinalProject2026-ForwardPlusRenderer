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

    private IntPtr _depthPrepassPipeline;
    private IntPtr _lightCullComputePipeline;

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

        IntPtr vertexShader = ShaderUtils.LoadShader(device, ShaderCross.ShaderStage.Vertex, "ForwardPlus/DepthPrepass");
        IntPtr fragmentShader = ShaderUtils.LoadShader(device, ShaderCross.ShaderStage.Fragment, "ForwardPlus/DepthPrepass");

        // TODO: A lot of this is reused from Material. Find a way to reduce reuse.
        const int numVertexAttributes = 1;
        SDL.GPUVertexAttribute* vertexAttributes = stackalloc SDL.GPUVertexAttribute[numVertexAttributes]
        {
            // Position
            new SDL.GPUVertexAttribute
            {
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float3,
                Location = 0,
                Offset = 0
            }
        };

        SDL.GPUVertexBufferDescription vertexBuffer = new()
        {
            Slot = 0,
            Pitch = (uint) sizeof(Vertex),
            InputRate = SDL.GPUVertexInputRate.Vertex,
            InstanceStepRate = 0
        };

        SDL.GPUVertexInputState vertexInput = new()
        {
            NumVertexAttributes = numVertexAttributes,
            VertexAttributes = (IntPtr) vertexAttributes,

            NumVertexBuffers = 1,
            VertexBufferDescriptions = new IntPtr(&vertexBuffer)
        };

        SDL.GPUColorTargetDescription colorTarget = new()
        {
            Format = renderer.RendererTargetFormat,
            BlendState = new SDL.GPUColorTargetBlendState
            {
                EnableBlend = false
            }
        };

        SDL.GPUGraphicsPipelineTargetInfo targetInfo = new()
        {
            NumColorTargets = 0,
            HasDepthStencilTarget = true,
            DepthStencilFormat = SDL.GPUTextureFormat.D32Float
        };

        SDL.GPUDepthStencilState depthStencilState = new()
        {
            EnableDepthTest = true,
            EnableDepthWrite = true,
            CompareOp = SDL.GPUCompareOp.LessOrEqual,
        };

        SDL.GPURasterizerState rasterizerState = new()
        {
            CullMode = SDL.GPUCullMode.Back,
            FrontFace = SDL.GPUFrontFace.CounterClockwise,
            FillMode = SDL.GPUFillMode.Fill
        };

        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            VertexInputState = vertexInput,
            TargetInfo = targetInfo,
            DepthStencilState = depthStencilState,
            RasterizerState = rasterizerState,
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList
        };

        _depthPrepassPipeline = SDL.CreateGPUGraphicsPipeline(device, in pipelineInfo)
            .Check("Create depth prepass pipeline");

        IntPtr spirv = ShaderUtils.CompileShader(ShaderCross.ShaderStage.Compute, "Shaders/ForwardPlus/LightCull.hlsl",
            "CSMain", out nuint spirvSize);

        ShaderCross.SPIRVInfo info = new()
        {
            ByteCode = spirv,
            ByteCodeSize = spirvSize,
            ManagedEntrypoint = "CSMain",
            ShaderStage = ShaderCross.ShaderStage.Compute
        };

        ShaderCross.ComputePipelineMetadata pipelineMetadata = new()
        {
            NumReadOnlyStorageBuffers = 1,
            //NumSamplers = 1,
            NumUniformBuffers = 1,
            ThreadCountX = 16,
            ThreadCountY = 16,
            ThreadCountZ = 1
        };

        _lightCullComputePipeline =
            ShaderCross.CompileComputePipelineFromSPIRV(device, in info, in pipelineMetadata, 0)
                .Check("Create compute pipeline");
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
        Vector3 cameraPos = inverseView.Translation;
        SceneData sceneData = new()
        {
            Camera = new ShaderCamera(camera.Projection, camera.View, new Vector4(cameraPos, 0)),
            NumLights = _numLights
        };
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        SDL.PushGPUFragmentUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        SDL.PushGPUComputeUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        
        SDL.GPUDepthStencilTargetInfo depthTarget = new()
        {
            Texture = depthTexture,
            LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store,
            ClearDepth = 1.0f
        };

        // Depth pre-pass
        IntPtr depthPrepass = SDL.BeginGPURenderPass(cb, 0, 0, in depthTarget).Check("Begin depth prepass");
        SDL.SetGPUViewport(depthPrepass, new SDL.GPUViewport
        {
            X = camera.Viewport.X,
            Y = camera.Viewport.Y,
            W = camera.Viewport.Width,
            H = camera.Viewport.Height,
            MinDepth = 0,
            MaxDepth = 1
        });
        
        // Order opaque objects to render front-to-back to reduce some level of overdraw.
        IOrderedEnumerable<(Renderable, Matrix4x4)> frontToBackOpaques =
            _opaques.OrderBy(tuple => Vector3.Distance(tuple.world.Translation, cameraPos));

        foreach ((Renderable renderable, Matrix4x4 world) in frontToBackOpaques)
        {
            SDL.PushGPUVertexUniformData(cb, 1, new IntPtr(&world), (uint) sizeof(Matrix4x4));
            SDL.BindGPUGraphicsPipeline(depthPrepass, _depthPrepassPipeline);
            
            SDL.GPUBufferBinding vertexBufferBinding = new()
            {
                Buffer = renderable.VertexBuffer,
                Offset = 0
            };
            SDL.BindGPUVertexBuffers(depthPrepass, 0, new IntPtr(&vertexBufferBinding), 1);

            SDL.GPUBufferBinding indexBufferBinding = new()
            {
                Buffer = renderable.IndexBuffer,
                Offset = 0
            };
            SDL.BindGPUIndexBuffer(depthPrepass, in indexBufferBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);

            SDL.DrawGPUIndexedPrimitives(depthPrepass, renderable.NumDraws, 1, 0, 0, 0);
        }
        
        SDL.EndGPURenderPass(depthPrepass);

        // Light culling compute pass
        { 
            IntPtr lightCullPass = SDL.BeginGPUComputePass(cb, [], 0, [], 0).Check("Begin light cull pass");
            
            SDL.BindGPUComputePipeline(lightCullPass, _lightCullComputePipeline);
            
            SDL.BindGPUComputeStorageBuffers(lightCullPass, 0, _lightBuffer, 1);
            //SDL.BindGPUComputeSamplers(lightCullPass, 0, );

            SDL.DispatchGPUCompute(lightCullPass, camera.Viewport.Width / 16, camera.Viewport.Height / 16, 1);
            
            SDL.EndGPUComputePass(lightCullPass);
        }
        
        SDL.GPUColorTargetInfo colorTarget = new()
        {
            Texture = colorTexture,
            ClearColor = new SDL.FColor(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, BackgroundColor.A),
            LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };

        depthTarget.LoadOp = SDL.GPULoadOp.Load;
        
        // Color render pass
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
        
        foreach ((Renderable renderable, Matrix4x4 world) in frontToBackOpaques)
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