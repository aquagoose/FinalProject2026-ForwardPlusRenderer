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

        _renderer = new ForwardPlusRenderer(Device);
        _cameras = [];
    }

    public void Dispose()
    {
        _renderer.Dispose();
        
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
        
        foreach (Camera camera in _cameras)
            _renderer.RenderCamera(cb, swapchainTexture, RendererTargetFormat, camera);
        
        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }

    internal unsafe void UpdateBuffer<T>(IntPtr buffer, uint offset, in ReadOnlySpan<T> data) where T : unmanaged
    {
        uint size = (uint) (data.Length * sizeof(T));
        
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
}