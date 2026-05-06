using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Structs;
using Renderer.Utils;
using SDL3;

namespace Renderer.Renderers;

internal class ForwardPlusRenderer : IRenderer
{
    // These constants must match the values in Shaders/ForwardPlus/Common.hlsli
    private const uint TileSize = 16;
    private const uint MaxLightsPerTile = 512;
    
    private readonly Renderer _renderer;
    private readonly List<(Renderable renderable, uint index)> _opaques;

    private ObjectData[] _objects;
    private uint _numObjects;
    private IntPtr _perObjectDataBuffer;

    private ShaderLight[] _lights;
    private uint _numLights;
    private IntPtr _lightBuffer;

    private IntPtr _depthPrepassPipeline;
    private IntPtr _lightCullComputePipeline;

    private uint _numTiles;
    private IntPtr _lightIndexBuffer;

    private readonly IntPtr _sampler;

    public bool ForwardPlusEnabled;
    
    public Color BackgroundColor { get; set; }

    public unsafe ForwardPlusRenderer(Renderer renderer)
    {
        _renderer = renderer;
        _opaques = [];
        ForwardPlusEnabled = true;

        IntPtr device = _renderer.Device;

        const uint defaultMaxObjects = 1024;
        _objects = new ObjectData[defaultMaxObjects];
        _perObjectDataBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.GraphicsStorageRead,
            defaultMaxObjects * (uint) sizeof(ObjectData));

        const uint defaultMaxLights = 1024;
        _lights = new ShaderLight[defaultMaxLights];
        _lightBuffer = SDLUtils.CreateBuffer(device,
            SDL.GPUBufferUsageFlags.GraphicsStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageRead,
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
            NumReadwriteStorageBuffers = 1,
            NumReadOnlyStorageTextures = 1,
            NumUniformBuffers = 1,
            ThreadCountX = 16,
            ThreadCountY = 16,
            ThreadCountZ = 1
        };

        _lightCullComputePipeline =
            ShaderCross.CompileComputePipelineFromSPIRV(device, in info, in pipelineMetadata, 0)
                .Check("Create compute pipeline");

        Size rendererSize = _renderer.Size;
        _numTiles = ((rendererSize.Width + TileSize - 1) * (rendererSize.Height + TileSize - 1)) / TileSize;
        _lightIndexBuffer = SDLUtils.CreateBuffer(device,
             SDL.GPUBufferUsageFlags.ComputeStorageWrite | SDL.GPUBufferUsageFlags.GraphicsStorageRead,
            (_numTiles * MaxLightsPerTile) * sizeof(uint));

        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            AddressModeU = SDL.GPUSamplerAddressMode.Repeat,
            AddressModeV = SDL.GPUSamplerAddressMode.Repeat,
            CompareOp = SDL.GPUCompareOp.GreaterOrEqual,
            MinFilter = SDL.GPUFilter.Nearest,
            MagFilter = SDL.GPUFilter.Nearest,
            MinLod = 0,
            MaxLod = float.MaxValue
        };

        _sampler = SDL.CreateGPUSampler(device, in samplerInfo).Check("Create sampler");
    }

    public void ClearDrawQueues()
    {
        _opaques.Clear();
        _numLights = 0;
    }

    public unsafe void AddOpaqueRenderable(Renderable renderable, in Matrix4x4 world)
    {
        if (_numObjects + 1 >= _objects.Length)
        {
            IntPtr device = _renderer.Device;
            
            Array.Resize(ref _objects, _objects.Length << 1);
            SDL.ReleaseGPUBuffer(device, _perObjectDataBuffer);
            _perObjectDataBuffer = SDLUtils.CreateBuffer(device, SDL.GPUBufferUsageFlags.GraphicsStorageRead,
                (uint) (_objects.Length * sizeof(ObjectData)));
        }
        
        Matrix4x4.Invert(world, out Matrix4x4 normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);

        uint index = _numObjects++;
        _objects[index] = new ObjectData(world, normalMatrix);
        
        _opaques.Add((renderable, index));
    }

    public unsafe void AddLight(ref readonly ShaderLight light)
    {
        if (_numLights + 1 >= _lights.Length)
        {
            IntPtr device = _renderer.Device;

            Array.Resize(ref _lights, _lights.Length << 1);
            SDL.ReleaseGPUBuffer(device, _lightBuffer);
            _lightBuffer = SDLUtils.CreateBuffer(device,
                SDL.GPUBufferUsageFlags.GraphicsStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageRead,
                (uint) (_lights.Length * sizeof(ShaderLight)));
        }

        _lights[_numLights++] = light;
    }

    public unsafe void RenderCamera(IntPtr cb, IntPtr colorTexture, IntPtr depthTexture, Camera camera, bool clear)
    {
        // TODO: UpdateBuffer with overload that accepts a command buffer.
        _renderer.UpdateBuffer(_perObjectDataBuffer, 0, _objects);
        _renderer.UpdateBuffer(_lightBuffer, 0, _lights);

        // TODO: Better way of doing this?
        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.View, out Matrix4x4 inverseView);
        Vector3 cameraPos = inverseView.Translation;
        SceneData sceneData = new()
        {
            Camera = new ShaderCamera(camera.Projection, inverseProjection, camera.View, new Vector4(cameraPos, 0)),
            NumLights = _numLights,
            TargetSize = _renderer.Size,
            UseLightIndices = ForwardPlusEnabled
        };
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        SDL.PushGPUFragmentUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));
        SDL.PushGPUComputeUniformData(cb, 0, new IntPtr(&sceneData), (uint) sizeof(SceneData));

        // Order opaque objects to render front-to-back to reduce some level of overdraw.
        IOrderedEnumerable<(Renderable, uint)> frontToBackOpaques =
            _opaques.OrderBy(tuple => Vector3.Distance(_objects[tuple.index].WorldMatrix.Translation, cameraPos));
        
        // Depth pre-pass
        {
            SDL.GPUDepthStencilTargetInfo depthTarget = new()
            {
                Texture = depthTexture,
                LoadOp = clear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
                StoreOp = SDL.GPUStoreOp.Store,
                ClearDepth = 1.0f
            };
            
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

            SDL.BindGPUVertexStorageBuffers(depthPrepass, 0, [_perObjectDataBuffer], 1);
            SDL.BindGPUFragmentStorageBuffers(depthPrepass, 0, [_perObjectDataBuffer], 1);

            SDL.BindGPUGraphicsPipeline(depthPrepass, _depthPrepassPipeline);

            foreach ((Renderable renderable, uint index) in frontToBackOpaques)
            {
                SDL.PushGPUVertexUniformData(cb, 1, new IntPtr(&index), sizeof(uint));

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
                SDL.BindGPUIndexBuffer(depthPrepass, in indexBufferBinding,
                    SDL.GPUIndexElementSize.IndexElementSize32Bit);

                SDL.DrawGPUIndexedPrimitives(depthPrepass, renderable.NumDraws, 1, 0, 0, 0);
            }

            SDL.EndGPURenderPass(depthPrepass);
        }

        // Light culling compute pass
        if (ForwardPlusEnabled)
        {
            IntPtr lightCullPass = SDL.BeginGPUComputePass(cb, [], 0, [
                new SDL.GPUStorageBufferReadWriteBinding
                {
                    Buffer = _lightIndexBuffer,
                    Cycle = 0
                }
            ], 1).Check("Begin light cull pass");

            SDL.BindGPUComputePipeline(lightCullPass, _lightCullComputePipeline);

            SDL.BindGPUComputeStorageTextures(lightCullPass, 0, [depthTexture], 1);
            SDL.BindGPUComputeStorageBuffers(lightCullPass, 0, (nint) Unsafe.AsPointer(ref _lightBuffer), 1);

            SDL.DispatchGPUCompute(lightCullPass, camera.Viewport.Width / 16, camera.Viewport.Height / 16, 1);

            SDL.EndGPUComputePass(lightCullPass);
        }

        {
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
                LoadOp = SDL.GPULoadOp.Load,
                StoreOp = SDL.GPUStoreOp.Store
            };

            // Color render pass
            IntPtr pass = SDL.BeginGPURenderPass(cb, new IntPtr(&colorTarget), 1, in depthTarget)
                .Check("Begin render pass");
            SDL.SetGPUViewport(pass, new SDL.GPUViewport
            {
                X = camera.Viewport.X,
                Y = camera.Viewport.Y,
                W = camera.Viewport.Width,
                H = camera.Viewport.Height,
                MinDepth = 0,
                MaxDepth = 1
            });

            nint* fragmentBuffers = stackalloc nint[]
            {
                _lightBuffer,
                _lightIndexBuffer
            };

            SDL.BindGPUVertexStorageBuffers(pass, 0, [_perObjectDataBuffer], 1);
            SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint) fragmentBuffers, 2);

            foreach ((Renderable renderable, uint index) in frontToBackOpaques)
            {
                Material material = renderable.Material;

                SDL.PushGPUVertexUniformData(cb, 1, new IntPtr(&index), sizeof(uint));

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
    }

    public void Resize(Size size)
    {

    }

    public void Dispose()
    {

    }
}
