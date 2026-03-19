using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Renderers;
using Renderer.Utils;
using SDL3;

namespace Renderer;

/// <summary>
/// The primary renderer used to draw and present 3D objects to the window.
/// </summary>
public class Renderer : IDisposable
{
    // 64mb transfer buffer
    private const uint TransferBufferSize = 64 * 1024 * 1024;
    
    private readonly IntPtr _window;
    // Global transfer buffer for all transfer operations.
    private readonly IntPtr _transferBuffer;
    private uint _currentTransferBufferOffset;

    private IntPtr _depthTexture;
    
    private readonly ISceneRenderer _renderer;
    private readonly List<Camera> _cameras;
    
    internal readonly IntPtr Device;

    internal SDL.GPUTextureFormat RendererTargetFormat => SDL.GetGPUSwapchainTextureFormat(Device, _window);

    /// <summary>
    /// A 1x1 white texture, useful when drawing lines or materials using vertex colors.
    /// </summary>
    public readonly Texture WhiteTexture;

    /// <summary>
    /// An empty normal map texture, useful when normal mapping is not used.
    /// </summary>
    public readonly Texture EmptyNormalTexture;

    /// <summary>
    /// The renderer's background color. This will always apply to the entire frame and cannot be changed per-camera.
    /// </summary>
    public Color BackgroundColor
    {
        get => _renderer.BackgroundColor;
        set => _renderer.BackgroundColor = value;
    }

    /// <summary>
    /// Create a <see cref="Renderer"/> from the given window.
    /// </summary>
    /// <param name="sdlWindow">The SDL3 window handle.</param>
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

        SDL.GetWindowSizeInPixels(_window, out int w, out int h);
        _depthTexture = SDLUtils.CreateTexture(Device, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureFormat.D32Float,
            (uint) w, (uint) h, SDL.GPUTextureUsageFlags.DepthStencilTarget);
        
        _renderer = new ForwardPlusRenderer(Device);
    }

    /// <summary>
    /// Dispose of this <see cref="Renderer"/>.
    /// </summary>
    public void Dispose()
    {
        _renderer.Dispose();
        SDL.ReleaseGPUTexture(Device, _depthTexture);
        
        WhiteTexture.Dispose();
        
        SDL.ReleaseGPUTransferBuffer(Device, _transferBuffer);
        
        SDL.ReleaseWindowFromGPUDevice(Device, _window);
        SDL.DestroyGPUDevice(Device);
    }

    /// <summary>
    /// Add a <see cref="Camera"/> to render the scene with.
    /// </summary>
    /// <param name="camera">The <see cref="Camera"/> to add.</param>
    /// <remarks>At least one camera must be added for the scene to render at all. The scene is not necessarily drawn in
    /// the order the cameras are added in.</remarks>
    public void AddCamera(in Camera camera)
    {
        _cameras.Add(camera);
    }

    /// <summary>
    /// Submit a <see cref="Renderable"/> for drawing.
    /// </summary>
    /// <param name="renderable">The <see cref="Renderable"/> to draw.</param>
    /// <param name="world">The world matrix.</param>
    public void Draw(Renderable renderable, in Matrix4x4 world)
    {
        _renderer.AddOpaqueRenderable(renderable, in world);
    }

    /// <summary>
    /// Clears various states and prepares the renderer for a new frame. You must call this to ensure the renderer's
    /// state is fit for rendering.
    /// </summary>
    public void NewFrame()
    {
        _renderer.ClearDrawQueues();
        _cameras.Clear();
    }

    /// <summary>
    /// Render and present everything to the window.
    /// </summary>
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
            _renderer.RenderCamera(cb, swapchainTexture, _depthTexture, camera, clear);
            clear = false;
        }

        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
    }

    /// <summary>
    /// Resize the renderer. This should be called when the window is resized.
    /// </summary>
    /// <param name="size">The <see cref="Size"/> in pixels.</param>
    public void Resize(Size size)
    {
        _renderer.Resize(size);
        // Recreate depth texture
        SDL.ReleaseGPUTexture(Device, _depthTexture);
        _depthTexture = SDLUtils.CreateTexture(Device, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureFormat.D32Float,
            size.Width, size.Height, SDL.GPUTextureUsageFlags.DepthStencilTarget);
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