using System.Numerics;
using Renderer.Primitives;
using Renderer.Renderers.Structs;
using Renderer.Utils;
using SDL3;

namespace Renderer.Skyboxes;

public class Skybox : IDisposable
{
    private readonly IntPtr _device;
    private readonly IntPtr _textureHandle;
    private readonly IntPtr _sampler;
    
    private readonly IntPtr _vertexBuffer;
    private readonly IntPtr _indexBuffer;
    private readonly IntPtr _pipeline;
    
    public unsafe Skybox(Renderer renderer, Bitmap right, Bitmap left, Bitmap top, Bitmap bottom, Bitmap front, Bitmap back)
    {
        _device = renderer.Device;

        Size size = right.Size;

        uint bpp = right.Format.BytesPerPixel;
        SDL.GPUTextureFormat format = right.Format switch
        {
            PixelFormat.RGBA8 => SDL.GPUTextureFormat.R8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException()
        };

        _textureHandle = SDLUtils.CreateTexture(_device, SDL.GPUTextureType.TextureTypeCube, format, size.Width,
            size.Height, SDLUtils.CalculateMipLevels(size.Width, size.Height), 6,
            SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget);
        
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, right.Data, 0);
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, left.Data, 1);
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, top.Data, 2);
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, bottom.Data, 3);
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, front.Data, 4);
        renderer.UpdateTexture(_textureHandle, 0, 0, size.Width, size.Height, bpp, back.Data, 5);
        
        renderer.GenerateMipmapsQueue.Enqueue(_textureHandle);

        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
            MinLod = 0,
            MaxLod = float.MaxValue
        };

        _sampler = SDL.CreateGPUSampler(_device, in samplerInfo).Check("Create sampler");

        Cube cube = new Cube();
        
        _vertexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Vertex,
            (uint) (cube.Vertices.Length * sizeof(Vertex)));
        _indexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Index,
            (uint) (cube.Indices.Length * sizeof(uint)));
        
        renderer.UpdateBuffer(_vertexBuffer, 0, cube.Vertices);
        renderer.UpdateBuffer(_indexBuffer, 0, cube.Indices);

        IntPtr vertexShader =
            ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Vertex, "Environment/Skybox/Skybox");
        IntPtr pixelShader =
            ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Fragment, "Environment/Skybox/Skybox");

        SDL.GPUColorTargetDescription colorTarget = new()
        {
            Format = renderer.RendererTargetFormat,
            BlendState = new SDL.GPUColorTargetBlendState
            {
                EnableBlend = false,
                ColorWriteMask = SDL.GPUColorComponentFlags.R | SDL.GPUColorComponentFlags.G |
                                 SDL.GPUColorComponentFlags.B | SDL.GPUColorComponentFlags.A
            }
        };

        SDL.GPUVertexBufferDescription vertexBufferDesc = new()
        {
            Slot = 0,
            Pitch = (uint) sizeof(Vertex),
            InputRate = SDL.GPUVertexInputRate.Vertex,
            InstanceStepRate = 0
        };
        
        // Only one!
        // Position
        SDL.GPUVertexAttribute vertexAttribute = new()
        {
            BufferSlot = 0,
            Location = 0,
            Offset = 0,
            Format = SDL.GPUVertexElementFormat.Float3
        };

        SDL.GPUVertexInputState vertexInput = new()
        {
            NumVertexBuffers = 1,
            VertexBufferDescriptions = new IntPtr(&vertexBufferDesc),
            NumVertexAttributes = 1,
            VertexAttributes = new IntPtr(&vertexAttribute)
        };
        
        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            VertexShader = vertexShader,
            FragmentShader = pixelShader,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo
            {
                NumColorTargets = 1,
                ColorTargetDescriptions = new IntPtr(&colorTarget),
                HasDepthStencilTarget = true,
                // TODO: renderer.DepthFormat
                DepthStencilFormat = SDL.GPUTextureFormat.D32Float
            },
            VertexInputState = vertexInput,
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
            DepthStencilState = new SDL.GPUDepthStencilState
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = SDL.GPUCompareOp.LessOrEqual
            },
            RasterizerState = new SDL.GPURasterizerState
            {
                CullMode = SDL.GPUCullMode.None,
                FrontFace = SDL.GPUFrontFace.CounterClockwise
            }
        };

        _pipeline = SDL.CreateGPUGraphicsPipeline(_device, in pipelineInfo).Check("Create pipeline");
        
        SDL.ReleaseGPUShader(_device, pixelShader);
        SDL.ReleaseGPUShader(_device, vertexShader);
    }

    public Skybox(Renderer renderer, string right, string left, string top, string bottom, string front, string back) :
        this(renderer, new Bitmap(right), new Bitmap(left), new Bitmap(top), new Bitmap(bottom), new Bitmap(front),
            new Bitmap(back)) { }

    internal unsafe void Draw(IntPtr cb, IntPtr colorTexture, IntPtr depthTexture, Camera camera)
    {
        ShaderCamera shaderCamera = new ShaderCamera(camera.Projection, camera.View, Vector4.Zero);
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&shaderCamera), (uint) sizeof(ShaderCamera));
        
        SDL.GPUColorTargetInfo colorTargetInfo = new()
        {
            Texture = colorTexture,
            LoadOp = SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };

        SDL.GPUDepthStencilTargetInfo depthTargetInfo = new()
        {
            Texture = depthTexture,
            LoadOp = SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };

        IntPtr pass = SDL.BeginGPURenderPass(cb, new IntPtr(&colorTargetInfo), 1, in depthTargetInfo)
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
        
        SDL.BindGPUGraphicsPipeline(pass, _pipeline);

        SDL.GPUTextureSamplerBinding textureBinding = new()
        {
            Texture = _textureHandle,
            Sampler = _sampler
        };
        SDL.BindGPUVertexSamplers(pass, 0, new IntPtr(&textureBinding), 1);
        SDL.BindGPUFragmentSamplers(pass, 0, new IntPtr(&textureBinding), 1);

        SDL.GPUBufferBinding vertexBuffer = new()
        {
            Buffer = _vertexBuffer,
            Offset = 0
        };
        SDL.BindGPUVertexBuffers(pass, 0, new IntPtr(&vertexBuffer), 1);

        SDL.GPUBufferBinding indexBuffer = new()
        {
            Buffer = _indexBuffer,
            Offset = 0
        };
        SDL.BindGPUIndexBuffer(pass, in indexBuffer, SDL.GPUIndexElementSize.IndexElementSize32Bit);

        SDL.DrawGPUIndexedPrimitives(pass, 36, 1, 0, 0, 0);
        
        SDL.EndGPURenderPass(pass);
    }

    public virtual void Dispose()
    {
        SDL.ReleaseGPUTexture(_device, _textureHandle);
    }
}