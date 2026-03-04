using SDL3;

namespace Renderer;

public class Renderer : IDisposable
{
    private readonly IntPtr _window;
    private readonly IntPtr _device;

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
        SDL.ReleaseWindowFromGPUDevice(_device, _window);
        SDL.DestroyGPUDevice(_device);
    }
}