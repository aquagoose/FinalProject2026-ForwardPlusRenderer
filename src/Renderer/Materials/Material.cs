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

    /// <summary>
    /// Construct a material and its GPU pipelines.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> to associate this material with.</param>
    /// <param name="vertexShader">The vertex shader name.</param>
    /// <param name="pixelShader">The pixel shader name.</param>
    /// <param name="numTextures">The number of textures the material will send to the pixel shader.</param>
    protected unsafe Material(Renderer renderer, ref readonly MaterialInfo info, string vertexShader,
        string pixelShader, uint numTextures)
    {
        _device = renderer.Device;
        TextureBindings = new SDL.GPUTextureSamplerBinding[numTextures];

        IntPtr vShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Vertex, vertexShader);
        IntPtr pShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Fragment, pixelShader);

        const int numVertexAttributes = 4;
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
            },
            
            // Normal
            new SDL.GPUVertexAttribute
            {
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float3,
                Location = 3,
                Offset = 36
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
            ColorTargetDescriptions = new IntPtr(&colorTarget),
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
            CullMode = info.CullFace switch
            {
                CullFace.None => SDL.GPUCullMode.None,
                CullFace.Front => SDL.GPUCullMode.Front,
                CullFace.Back => SDL.GPUCullMode.Back,
                _ => throw new ArgumentOutOfRangeException()
            },
            FrontFace = info.WindingOrder switch
            {
                WindingOrder.CounterClockwise => SDL.GPUFrontFace.CounterClockwise,
                WindingOrder.Clockwise => SDL.GPUFrontFace.Clockwise,
                _ => throw new ArgumentOutOfRangeException()
            },
            FillMode = SDL.GPUFillMode.Fill
        };

        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            VertexShader = vShader,
            FragmentShader = pShader,
            VertexInputState = vertexInput,
            TargetInfo = targetInfo,
            DepthStencilState = depthStencilState,
            RasterizerState = rasterizerState,
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList
        };

        Pipeline = SDL.CreateGPUGraphicsPipeline(_device, in pipelineInfo).Check("Create pipeline");
        
        SDL.ReleaseGPUShader(_device, pShader);
        SDL.ReleaseGPUShader(_device, vShader);
    }
    
    /// <summary>
    /// Dispose of this <see cref="Material"/>.
    /// </summary>
    public virtual void Dispose()
    {
        SDL.ReleaseGPUGraphicsPipeline(_device, Pipeline);
    }

    /// <summary>
    /// Populate the texture bindings array with a texture + sampler.
    /// </summary>
    /// <param name="bindings">The bindings array.</param>
    /// <remarks>This array length will <b>always</b> the same length as the numTextures provided in the constructor.</remarks>
    protected abstract void PopulateTextureBindings(ref SDL.GPUTextureSamplerBinding[] bindings);
}