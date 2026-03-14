using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Renderers;
using Renderer.Utils;
using SDL3;

namespace Renderer;

public class Renderer : IDisposable
{
    // 64mb transfer buffer
    private const uint TransferBufferSize = 64 * 1024 * 1024;
    
    private readonly IntPtr _window;
    // Global transfer buffer for all transfer operations.
    private readonly IntPtr _transferBuffer;
    private readonly uint _currentTransferBufferOffset;
    
    private readonly IRenderer _renderer;
    private readonly List<Camera> _cameras;
    
    internal readonly IntPtr Device;

    internal SDL.GPUTextureFormat RendererTargetFormat => SDL.GetGPUSwapchainTextureFormat(Device, _window);

    public readonly Texture WhiteTexture;

    public Renderer(IntPtr sdlWindow)
    {
        _window = sdlWindow;

        uint deviceProps = SDL.CreateProperties();
        // Enable Vulkan. Vulkan should be supported on every platform (including macOS with MoltenVK) so it's a good fallback.
        SDL.SetBooleanProperty(deviceProps, SDL.Props.GPUDeviceCreateShadersSPIRVBoolean, true);

        // Enable D3D12 (using DXIL) on Windows.
        if (OperatingSystem.IsWindows())
            SDL.SetBooleanProperty(deviceProps, SDL.Props.GPUDeviceCreateShadersDXILBoolean, true);

        // Enable Metal on macOS
        if (OperatingSystem.IsMacOS())
            SDL.SetBooleanProperty(deviceProps, SDL.Props.GPUDeviceCreateShadersMSLBoolean, true);
        
#if DEBUG
        SDL.SetBooleanProperty(deviceProps, SDL.Props.GPUDeviceCreateDebugModeBoolean, true);
        // This is specifically for my ThinkPad where it defaults to the dedicated GPU, which uses more power and has
        // a longer startup time, not great during development.
        SDL.SetBooleanProperty(deviceProps, SDL.Props.GPUDeviceCreatePreferLowPowerBoolean, true);
#endif

        Device = SDL.CreateGPUDeviceWithProperties(deviceProps).Check("Create device");
        SDL.ClaimWindowForGPUDevice(Device, _window).Check("Claim window for device");

        _transferBuffer = SDLUtils.CreateTransferBuffer(Device, SDL.GPUTransferBufferUsage.Upload, TransferBufferSize);
        
        _cameras = [];
        WhiteTexture = new Texture(this, [255, 255, 255, 255], new Size(1), PixelFormat.RGBA8);
        
        _renderer = new ForwardPlusRenderer(Device);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        WhiteTexture.Dispose();
        
        SDL.ReleaseGPUTransferBuffer(Device, _transferBuffer);
        
        SDL.ReleaseWindowFromGPUDevice(Device, _window);
        SDL.DestroyGPUDevice(Device);
    }

    public void AddCamera(in Camera camera)
    {
        _cameras.Add(camera);
    }

    public void Draw(Renderable renderable, in Matrix4x4 world)
    {
        _renderer.AddOpaqueRenderable(renderable, in world);
    }

    public void NewFrame()
    {
        _renderer.ClearDrawQueues();
        _cameras.Clear();
    }

    public void Render()
    {
        IntPtr cb = SDL.AcquireGPUCommandBuffer(Device).Check("Acquire command buffer");

        SDL.WaitAndAcquireGPUSwapchainTexture(cb, _window, out IntPtr swapchainTexture, out _, out _)
            .Check("Acquire swapchain texture");

        // Don't render anything if SDL doesn't give us anything to render to.
        if (swapchainTexture == IntPtr.Zero)
        {
            SDL.CancelGPUCommandBuffer(cb);
            return;
        }

        bool clear = true;
        foreach (Camera camera in _cameras)
        {
            _renderer.RenderCamera(cb, swapchainTexture, RendererTargetFormat, camera, clear);
            clear = false;
        }

        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }

    internal unsafe void UpdateBuffer<T>(IntPtr buffer, uint offset, in ReadOnlySpan<T> data) where T : unmanaged
    {
        uint size = (uint) (data.Length * sizeof(T));
        
        // TODO: Don't cycle the buffer!
        void* transferPtr = (void*) SDL.MapGPUTransferBuffer(Device, _transferBuffer, true);
        fixed (T* pData = data)
        {
            Unsafe.CopyBlock((byte*) transferPtr + _currentTransferBufferOffset, pData, size);
        }
        SDL.UnmapGPUTransferBuffer(Device, _transferBuffer);

        IntPtr cb = SDL.AcquireGPUCommandBuffer(Device).Check("Acquire command buffer");
        IntPtr pass = SDL.BeginGPUCopyPass(cb).Check("Begin copy pass");

        SDL.GPUTransferBufferLocation src = new()
        {
            TransferBuffer = _transferBuffer,
            Offset = _currentTransferBufferOffset
        };

        SDL.GPUBufferRegion dest = new()
        {
            Buffer = buffer,
            Offset = offset,
            Size = size
        };
        
        SDL.UploadToGPUBuffer(pass, in src, in dest, false);
        
        SDL.EndGPUCopyPass(pass);
        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }

    internal unsafe void UpdateTexture<T>(IntPtr texture, uint x, uint y, uint width, uint height, uint bytesPerPixel,
        in ReadOnlySpan<T> data) where T : unmanaged
    {
        uint size = width * height * bytesPerPixel;
        
        // TODO: Don't cycle the buffer!
        void* transferPtr = (void*) SDL.MapGPUTransferBuffer(Device, _transferBuffer, true);
        fixed (T* pData = data)
        {
            Unsafe.CopyBlock((byte*) transferPtr + _currentTransferBufferOffset, pData, size);
        }
        SDL.UnmapGPUTransferBuffer(Device, _transferBuffer);

        IntPtr cb = SDL.AcquireGPUCommandBuffer(Device).Check("Acquire command buffer");
        IntPtr pass = SDL.BeginGPUCopyPass(cb).Check("Begin copy pass");

        SDL.GPUTextureTransferInfo src = new()
        {
            TransferBuffer = _transferBuffer,
            Offset = _currentTransferBufferOffset,
            PixelsPerRow = width,
            RowsPerLayer = height
        };

        SDL.GPUTextureRegion dest = new()
        {
            Texture = texture,
            X = x,
            Y = x,
            Z = 0,
            W = width,
            H = height,
            D = 1,
            Layer = 0,
            MipLevel = 0
        };
        
        SDL.UploadToGPUTexture(pass, in src, in dest, false);
        
        SDL.EndGPUCopyPass(pass);
        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }
}