using Renderer.Math;
using SDL3;

namespace Renderer.Tests.Common;

public class TestBase(string testName) : IDisposable
{
    private IntPtr _window;
    private Renderer _renderer = null!;
    private bool _running;

    public Renderer Renderer => _renderer;

    public Size Size
    {
        get
        {
            SDL.GetWindowSizeInPixels(_window, out int w, out int h);
            return new Size((uint) w, (uint) h);
        }
    }

    protected virtual void Load() { }

    protected virtual void Loop(float dt) { }

    public void Run()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
            throw new Exception($"Failed to initialize SDL: {SDL.GetError()}");

        _window = SDL.CreateWindow(testName, 1280, 720, SDL.WindowFlags.HighPixelDensity | SDL.WindowFlags.Resizable);
        if (_window == IntPtr.Zero)
            throw new Exception($"Failed to create window: {SDL.GetError()}");

        _renderer = new Renderer(_window);

        Load();
        
        _running = true;
        while (_running)
        {
            _renderer.NewFrame();
            
            while (SDL.PollEvent(out SDL.Event sdlEvent))
            {
                switch ((SDL.EventType) sdlEvent.Type)
                {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        _running = false;
                        break;
                    
                    case SDL.EventType.WindowResized:
                        _renderer.Resize(Size);
                        break;
                }
            }
            
            Loop(1.0f / 60.0f);
            _renderer.Render();
        }
    }

    public virtual void Dispose()
    {
        _renderer.Dispose();
        SDL.DestroyWindow(_window);
        SDL.Quit();
    }
}