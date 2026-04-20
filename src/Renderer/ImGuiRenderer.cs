using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Renderer.Math;
using Renderer.Utils;
using SDL3;

namespace Renderer;

// Dear ImGui renderer, modified from the ones I wrote for the Crimson Engine and Glimpse, for time sake.
// https://github.com/Aquatic-Games/CrimsonEngine/blob/main/src/Crimson.Graphics/Renderers/ImGuiRenderer.cs
// https://github.com/aquagoose/Glimpse/blob/main/src/Glimpse/Graphics/ImGuiRenderer.cs
internal sealed class ImGuiRenderer : IDisposable
{
    private readonly Renderer _renderer;
    private readonly IntPtr _device;
    
    private readonly ImGuiContextPtr _imguiContext;

    private uint _vBufferSize;
    private uint _iBufferSize;

    private IntPtr _vertexBuffer;
    private IntPtr _indexBuffer;
    private IntPtr _transferBuffer;
    private uint _transferBufferSize;

    private IntPtr _pipeline;
    private IntPtr _sampler;

    public ImGuiContextPtr Context => _imguiContext;
    
    public unsafe ImGuiRenderer(Renderer renderer, Size size, SDL.GPUTextureFormat outFormat)
    {
        _renderer = renderer;
        _device = _renderer.Device;

        _imguiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imguiContext);

        _vBufferSize = 5000;
        _iBufferSize = 10000;

        uint vBufferSizeBytes = (uint) (_vBufferSize * sizeof(ImDrawVert));
        uint iBufferSizeBytes = _iBufferSize * sizeof(uint);
        
        _vertexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Vertex, vBufferSizeBytes);
        _indexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Index, iBufferSizeBytes);

        _transferBufferSize = vBufferSizeBytes + iBufferSizeBytes;
        _transferBuffer = SDLUtils.CreateTransferBuffer(_device, SDL.GPUTransferBufferUsage.Upload, _transferBufferSize);

        IntPtr vertexShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Vertex, "ImGui");
        IntPtr pixelShader = ShaderUtils.LoadShader(_device, ShaderCross.ShaderStage.Fragment, "ImGui");

        SDL.GPUColorTargetDescription targetDesc = new()
        {
            Format = outFormat,
            BlendState = new SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
                ColorBlendOp = SDL.GPUBlendOp.Add,
                SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = SDL.GPUBlendOp.Add,
                SrcAlphaBlendFactor = SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL.GPUBlendFactor.One
            }
        };

        SDL.GPUVertexBufferDescription vertexBufferDesc = new()
        {
            InputRate = SDL.GPUVertexInputRate.Vertex,
            Slot = 0,
            InstanceStepRate = 0,
            Pitch = (uint) sizeof(ImDrawVert)
        };

        SDL.GPUVertexAttribute* vertexAttributes = stackalloc SDL.GPUVertexAttribute[]
        {
            new SDL.GPUVertexAttribute // Position
                { Format = SDL.GPUVertexElementFormat.Float2, Offset = 0, BufferSlot = 0, Location = 0 },
            new SDL.GPUVertexAttribute // TexCoord
                { Format = SDL.GPUVertexElementFormat.Float2, Offset = 8, BufferSlot = 0, Location = 1 },
            new SDL.GPUVertexAttribute // Color
                { Format = SDL.GPUVertexElementFormat.Ubyte4Norm, Offset = 16, BufferSlot = 0, Location = 2 }
        };

        SDL.GPUGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            VertexShader = vertexShader,
            FragmentShader = pixelShader,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
            {
                NumColorTargets = 1,
                ColorTargetDescriptions = new IntPtr(&targetDesc)
            },
            VertexInputState = new SDL.GPUVertexInputState()
            {
                NumVertexBuffers = 1,
                VertexBufferDescriptions = new IntPtr(&vertexBufferDesc),
                NumVertexAttributes = 3,
                VertexAttributes = (nint) vertexAttributes
            },
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList
        };

        _pipeline = SDL.CreateGPUGraphicsPipeline(_device, in pipelineInfo).Check("Create pipeline");
        
        SDL.ReleaseGPUShader(_device, pixelShader);
        SDL.ReleaseGPUShader(_device, vertexShader);

        SDL.GPUSamplerCreateInfo samplerInfo = new()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.Repeat,
            AddressModeV = SDL.GPUSamplerAddressMode.Repeat,
            MaxLod = 1000
        };

        _sampler = SDL.CreateGPUSampler(_device, in samplerInfo).Check("Create sampler");

        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(size.Width, size.Height);
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures;
        io.IniFilename = null;
        io.LogFilename = null;
    }

    public unsafe bool Render(IntPtr cb, IntPtr colorTarget, bool shouldClear)
    {
        ImGui.Render();
        ImDrawDataPtr drawData = ImGui.GetDrawData();

        // Don't bother rendering if there is nothing to draw.
        if (drawData.CmdListsCount == 0)
            return false;
        
        //SdlUtils.PushDebugGroup(cb, "ImGUI Buffer Copy");

        bool hasResizedBuffer = false;
        
        if (drawData.TotalVtxCount >= _vBufferSize)
        {
            SDL.ReleaseGPUBuffer(_device, _vertexBuffer);
            _vBufferSize = (uint) drawData.TotalVtxCount + 5000;
            _vertexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Vertex,
                (uint) (_vBufferSize * sizeof(ImDrawVert)));
            hasResizedBuffer = true;
        }
        
        if (drawData.TotalIdxCount >= _iBufferSize)
        {
            SDL.ReleaseGPUBuffer(_device, _indexBuffer);
            _iBufferSize = (uint) drawData.TotalIdxCount + 10000;
            _indexBuffer = SDLUtils.CreateBuffer(_device, SDL.GPUBufferUsageFlags.Index, _iBufferSize * sizeof(uint));
            hasResizedBuffer = true;
        }

        if (hasResizedBuffer)
        {
            uint vBufferSizeBytes = (uint) (_vBufferSize * sizeof(ImDrawVert));
            uint iBufferSizeBytes = _iBufferSize * sizeof(uint);
            
            SDL.ReleaseGPUTransferBuffer(_device, _transferBuffer);
            _transferBufferSize = vBufferSizeBytes + iBufferSizeBytes;
            _transferBuffer = SDLUtils.CreateTransferBuffer(_device, SDL.GPUTransferBufferUsage.Upload, _transferBufferSize);
        }

        uint vertexOffset = 0;
        uint indexOffset = 0;

        void* mappedPtr =
            (void*) SDL.MapGPUTransferBuffer(_device, _transferBuffer, true).Check("Map transfer buffer");
        
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            uint vertexSize = (uint) (cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            uint indexSize = (uint) (cmdList.IdxBuffer.Size * sizeof(ushort));

            Unsafe.CopyBlock((byte*) mappedPtr + vertexOffset, cmdList.VtxBuffer.Data, vertexSize);
            Unsafe.CopyBlock((byte*) mappedPtr + (_vBufferSize * sizeof(ImDrawVert)) + indexOffset,
                cmdList.IdxBuffer.Data, indexSize);

            vertexOffset += vertexSize;
            indexOffset += indexSize;
        }
        
        SDL.UnmapGPUTransferBuffer(_device, _transferBuffer);

        IntPtr copyPass = SDL.BeginGPUCopyPass(cb).Check("Begin copy pass");

        SDL.GPUTransferBufferLocation vertexSource = new()
        {
            TransferBuffer = _transferBuffer,
            Offset = 0
        };

        SDL.GPUBufferRegion vertexDest = new()
        {
            Buffer = _vertexBuffer,
            Offset = 0,
            Size = vertexOffset
        };
        
        SDL.UploadToGPUBuffer(copyPass, in vertexSource, in vertexDest, false);

        SDL.GPUTransferBufferLocation indexSource = new()
        {
            TransferBuffer = _transferBuffer,
            Offset = _vBufferSize * (uint) sizeof(ImDrawVert)
        };

        SDL.GPUBufferRegion indexDest = new()
        {
            Buffer = _indexBuffer,
            Offset = 0,
            Size = indexOffset
        };
        
        SDL.UploadToGPUBuffer(copyPass, in indexSource, in indexDest, false);
        
        SDL.EndGPUCopyPass(copyPass);

        ref ImVector<ImTextureDataPtr> textures = ref drawData.Textures;
        for (int i = 0; i < textures.Size; i++)
        {
            ImTextureDataPtr texture = textures[i];
            if (texture.Status != ImTextureStatus.Ok)
                UpdateTexture(texture);
        }
        
        //SdlUtils.PopDebugGroup(cb);

        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(drawData.DisplayPos.X,
            drawData.DisplayPos.X + drawData.DisplaySize.X, drawData.DisplayPos.Y + drawData.DisplaySize.Y,
            drawData.DisplayPos.Y, -1, 1);
        
        SDL.PushGPUVertexUniformData(cb, 0, new IntPtr(&projection), 64);
        
        //SdlUtils.PushDebugGroup(cb, "ImGUI Pass");

        SDL.GPUColorTargetInfo targetInfo = new()
        {
            Texture = colorTarget,
            ClearColor = new SDL.FColor(0.0f, 0.0f, 0.0f, 1.0f),
            LoadOp = shouldClear ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store
        };

        IntPtr renderPass = SDL.BeginGPURenderPass(cb, new IntPtr(&targetInfo), 1, IntPtr.Zero)
            .Check("Begin render pass");
        
        SDL.BindGPUGraphicsPipeline(renderPass, _pipeline);

        SDL.GPUViewport viewport = new()
        {
            X = drawData.DisplayPos.X,
            Y = drawData.DisplayPos.Y,
            W = drawData.DisplaySize.X,
            H = drawData.DisplaySize.Y,
            MinDepth = 0,
            MaxDepth = 1
        };
        SDL.SetGPUViewport(renderPass, in viewport);

        SDL.GPUBufferBinding vertexBinding = new()
        {
            Buffer = _vertexBuffer,
            Offset = 0
        };
        
        SDL.BindGPUVertexBuffers(renderPass, 0, new IntPtr(&vertexBinding), 1);

        SDL.GPUBufferBinding indexBinding = new()
        {
            Buffer = _indexBuffer,
            Offset = 0
        };

        SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize16Bit);

        vertexOffset = 0;
        indexOffset = 0;
        Vector2 clipOff = drawData.DisplayPos;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
            {
                ImDrawCmd drawCmd = cmdList.CmdBuffer[j];
                
                if (drawCmd.UserCallback != null)
                    continue;
                
                Vector2 clipMin = new Vector2(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
                Vector2 clipMax = new Vector2(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);
                
                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                SDL.Rect scissorRect = new()
                {
                    X = (int) clipMin.X,
                    Y = (int) clipMin.Y,
                    W = (int) clipMax.X - (int) clipMin.X,
                    H = (int) clipMax.Y - (int) clipMin.Y
                };

                SDL.SetGPUScissor(renderPass, in scissorRect);

                SDL.GPUTextureSamplerBinding samplerBinding = new()
                {
                    Texture = drawCmd.GetTexID(),
                    Sampler = _sampler
                };

                // Even though the samplers aren't used in the vertex shader, SDL_GPU thinks they are, so this is a dumb
                // workaround for this.
                SDL.BindGPUVertexSamplers(renderPass, 0, new IntPtr(&samplerBinding), 1);
                SDL.BindGPUFragmentSamplers(renderPass, 0, new IntPtr(&samplerBinding), 1);

                SDL.DrawGPUIndexedPrimitives(renderPass, drawCmd.ElemCount, 1, drawCmd.IdxOffset + indexOffset,
                    (short) (drawCmd.VtxOffset + vertexOffset), 0);
            }
            
            vertexOffset += (uint) cmdList.VtxBuffer.Size;
            indexOffset += (uint) cmdList.IdxBuffer.Size;
        }
        
        SDL.EndGPURenderPass(renderPass);
        
        //SdlUtils.PopDebugGroup(cb);

        return true;
    }

    public void Resize(Size size)
    {
        ImGui.GetIO().DisplaySize = new Vector2(size.Width, size.Height);
    }

    private unsafe void UpdateTexture(ImTextureDataPtr textureData)
    {
        Console.WriteLine(textureData.Status);
        
        switch (textureData.Status)
        {
            case ImTextureStatus.WantCreate:
            {
                IntPtr texture = SDLUtils.CreateTexture(_device, SDL.GPUTextureType.TextureType2D,
                    SDL.GPUTextureFormat.R8G8B8A8Unorm, (uint) textureData.Width, (uint) textureData.Height, 1, 1,
                    SDL.GPUTextureUsageFlags.Sampler);
                
                _renderer.UpdateTexture(texture, 0, 0, (uint) textureData.Width, (uint) textureData.Height,
                    (uint) textureData.BytesPerPixel, textureData.Pixels);

                textureData.TexID = texture;
                textureData.Status = ImTextureStatus.Ok;
                
                break;
            }
            
            case ImTextureStatus.WantUpdates:
            {
                // For reasons that I don't quite understand and don't have time to work out (I suspect its due to
                // the way ImGui packs its texture data), the texture uploads aren't compatible with Renderer.UpdateTexture
                // and lead to garbage results. I've re-implemented the way the official ImGui backend does it, and it
                // works fine: https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_sdlgpu3.cpp#L353
                ImTextureRect rect = textureData.UpdateRect;

                uint uploadPitch = (uint) (rect.W * textureData.BytesPerPixel);
                uint uploadSize = (uint) (rect.W * rect.H * textureData.BytesPerPixel);
                
                if (uploadSize > _transferBufferSize)
                {
                    SDL.ReleaseGPUTransferBuffer(_device, _transferBuffer);
                    _transferBufferSize = uploadSize + 1024;
                    _transferBuffer = SDLUtils.CreateTransferBuffer(_device, SDL.GPUTransferBufferUsage.Upload,
                        _transferBufferSize);
                }

                void* transferPtr = (void*) SDL.MapGPUTransferBuffer(_device, _transferBuffer, true);
                for (int y = 0; y < rect.H; y++)
                {
                    Unsafe.CopyBlock((byte*) transferPtr + y * uploadPitch, textureData.GetPixelsAt(rect.X, rect.Y + y),
                        uploadPitch);
                }
                SDL.UnmapGPUTransferBuffer(_device, _transferBuffer);

                SDL.GPUTextureTransferInfo transferInfo = new()
                {
                    TransferBuffer = _transferBuffer,
                    Offset = 0
                };

                SDL.GPUTextureRegion region = new()
                {
                    Texture = textureData.TexID,
                    X = rect.X,
                    Y = rect.Y,
                    W = rect.W,
                    H = rect.H,
                    D = 1
                };

                IntPtr cb = SDL.AcquireGPUCommandBuffer(_device).Check("Acquire command buffer");
                IntPtr pass = SDL.BeginGPUCopyPass(cb).Check("Begin copy pass");
                SDL.UploadToGPUTexture(pass, in transferInfo, in region, false);
                SDL.EndGPUCopyPass(pass);
                SDL.SubmitGPUCommandBuffer(cb);

                textureData.Status = ImTextureStatus.Ok;

                break;
            }

            case ImTextureStatus.WantDestroy:
            {
                SDL.ReleaseGPUTexture(_device, textureData.TexID);
                textureData.TexID = ImTextureID.Null;
                textureData.Status = ImTextureStatus.Destroyed;
                break;
            }
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Dispose()
    {
        ref ImVector<ImTextureDataPtr> textures = ref ImGui.GetPlatformIO().Textures;
        for (int i = 0; i < textures.Size; i++)
        {
            textures[i].Status = ImTextureStatus.Destroyed;
            UpdateTexture(textures[i]);
        }
        
        SDL.ReleaseGPUSampler(_device, _sampler);
        SDL.ReleaseGPUGraphicsPipeline(_device, _pipeline);
        SDL.ReleaseGPUTransferBuffer(_device, _transferBuffer);
        SDL.ReleaseGPUBuffer(_device, _indexBuffer);
        SDL.ReleaseGPUBuffer(_device, _vertexBuffer);
        
        ImGui.DestroyContext(_imguiContext);
    }
}