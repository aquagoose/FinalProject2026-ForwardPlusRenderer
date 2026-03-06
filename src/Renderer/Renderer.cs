using Renderer.Renderers;
using SDL3;

namespace Renderer;

public class Renderer : IDisposable
{
    private readonly IntPtr _window;
    private readonly IntPtr _device;

    private readonly IRenderer _renderer;

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

        _device = SDL.CreateGPUDeviceWithProperties(deviceProps).Check("Create device");
        
        SDL.ClaimWindowForGPUDevice(_device, _window).Check("Claim window for device");
    }

    public void Dispose()
    {
        _renderer.Dispose();
        SDL.ReleaseWindowFromGPUDevice(_device, _window);
        SDL.DestroyGPUDevice(_device);
    }

    public unsafe void Render()
    {
        IntPtr cb = SDL.AcquireGPUCommandBuffer(_device).Check("Acquire command buffer");

        SDL.WaitAndAcquireGPUSwapchainTexture(cb, _window, out IntPtr swapchainTexture, out _, out _)
            .Check("Acquire swapchain texture");

        // Don't render anything if SDL doesn't give us anything to render to.
        if (swapchainTexture == IntPtr.Zero)
        {
            SDL.CancelGPUCommandBuffer(cb);
            return;
        }

        SDL.GPUColorTargetInfo colorTarget = new()
        {
            Texture = swapchainTexture,
            ClearColor = new SDL.FColor(1.0f, 0.5f, 0.25f, 1.0f),
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store
        };
        IntPtr renderPass = SDL.BeginGPURenderPass(cb, new IntPtr(&colorTarget), 1, IntPtr.Zero)
            .Check("Begin render pass");
        
        SDL.EndGPURenderPass(renderPass);
        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }
}