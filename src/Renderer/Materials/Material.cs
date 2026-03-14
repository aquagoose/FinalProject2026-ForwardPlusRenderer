using Renderer.Utils;
using SDL3;

namespace Renderer.Materials;

/// <summary>
/// A material describes how an object is rendered.
/// </summary>
public abstract class Material : IDisposable
{
    private readonly IntPtr _device;

    internal readonly IntPtr Pipeline;

    internal SDL.GPUTextureSamplerBinding[] TextureBindings
    {
        get
        {
            PopulateTextureBindings(ref field);
            return field;
        }
    }

    protected unsafe Material(Renderer renderer, string vertexShader, string pixelShader, uint numTextures)
    {
        _device = renderer.Device;
        TextureBindings = new SDL.GPUTextureSamplerBinding[numTextures];

        IntPtr vShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Vertex, vertexShader);
        IntPtr pShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Fragment, pixelShader);

        const int numVertexAttributes = 3;
        SDL.GPUVertexAttribute* vertexAttributes = stackalloc SDL.GPUVertexAttribute[numVertexAttributes]
        {
            // Position
            new SDL.GPUVertexAttribute
            {
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float3,
                Location = 0,
                Offset = 0
            },
            
            // TexCoord
            new SDL.GPUVertexAttribute
            {
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float2,
                Location = 1,
                Offset = 12
            },
            
            // Color
            new SDL.GPUVertexAttribute
            {
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float4,
                Location = 2,
                Offset = 20
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
                EnableBlend = false,
                ColorWriteMask = SDL.GPUColorComponentFlags.R | SDL.GPUColorComponentFlags.G |
                                 SDL.GPUColorComponentFlags.B | SDL.GPUColorComponentFlags.A
            }
        };

        SDL.GPUGraphicsPipelineTargetInfo targetInfo = new()
        {
            NumColorTargets = 1,
            ColorTargetDescriptions = new IntPtr(&colorTarget)
        };

        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            VertexShader = vShader,
            FragmentShader = pShader,
            VertexInputState = vertexInput,
            TargetInfo = targetInfo,
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList
        };

        Pipeline = SDL.CreateGPUGraphicsPipeline(_device, in pipelineInfo).Check("Create pipeline");
        
        SDL.ReleaseGPUShader(_device, pShader);
        SDL.ReleaseGPUShader(_device, vShader);
    }
    
    public virtual void Dispose()
    {
        SDL.ReleaseGPUGraphicsPipeline(_device, Pipeline);
    }

    protected internal abstract void PopulateTextureBindings(ref SDL.GPUTextureSamplerBinding[] bindings);
}