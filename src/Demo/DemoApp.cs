using System.Diagnostics;
using Demo.Demos;
using Renderer.Math;
using SDL3;

namespace Demo;

public static class DemoApp
{
    private static IntPtr _sdlWindow;
    private static bool _alive;
    private static Renderer.Renderer _renderer = null!;
    private static Demos.Demo _currentDemo = null!;
    private static Demos.Demo? _demoToSwitch = null;

    public static Renderer.Renderer Renderer => _renderer;

    public static Size WindowSize
    {
        get
        {
            SDL.GetWindowSizeInPixels(_sdlWindow, out int w, out int h);
            return new Size((uint) w, (uint) h);
        }
    }

    public static void Run()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
            throw new Exception($"Startup failure: Failed to initialize SDL: {SDL.GetError()}");

        SDL.WindowFlags flags = SDL.WindowFlags.Resizable | SDL.WindowFlags.HighPixelDensity;
#if RELEASE
        flags |= SDL.WindowFlags.Fullscreen;
#endif

        _sdlWindow = SDL.CreateWindow("Renderer Demo", 1280, 720, flags);
        if (_sdlWindow == IntPtr.Zero)
            throw new Exception($"Startup failure: Failed to create window: {SDL.GetError()}");

        _renderer = new Renderer.Renderer(_sdlWindow);

        _currentDemo = new LightCasterDemo();
        _currentDemo.Initialize();

        Stopwatch sw = Stopwatch.StartNew();
        
        _alive = true;
        while (_alive)
        {
            while (SDL.PollEvent(out SDL.Event winEvent))
            {
                switch ((SDL.EventType) winEvent.Type)
                {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        _alive = false;
                        break;
                }
            }

            float dt = (float) sw.Elapsed.TotalSeconds;
            sw.Restart();
            
            Renderer.NewFrame();
            
            _currentDemo.Update(dt);
            _currentDemo.Draw();
            
            Renderer.Render();
        }
        
        Renderer.Dispose();
        SDL.DestroyWindow(_sdlWindow);
        SDL.Quit();
    }
}