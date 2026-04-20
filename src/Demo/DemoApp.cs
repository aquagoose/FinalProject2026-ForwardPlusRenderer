global using Key = SDL3.SDL.Keycode;
global using MouseButton = SDL3.SDL.MouseButtonFlags;
using System.Diagnostics;
using System.Numerics;
using Demo.Demos;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;
using SDL3;

namespace Demo;

public static class DemoApp
{
    public const float ActivityTimeout = 30;
    
    private static IntPtr _sdlWindow;
    private static bool _alive;
    private static float _activityTimer;
    
    private static HashSet<Key> _keysDown = [];
    private static HashSet<Key> _keysPressed = [];
    private static Vector2 _mouseDelta;
    
    private static Renderer.Renderer _renderer = null!;
    private static Demos.Demo _currentDemo = null!;
    private static Demos.Demo? _demoToSwitch = null;

    private static Texture[] _backgroundTextures = null!;
    
    public static Renderer.Renderer Renderer => _renderer;

    public static Texture[] BackgroundTextures => _backgroundTextures;

    public static Size WindowSize
    {
        get
        {
            SDL.GetWindowSizeInPixels(_sdlWindow, out int w, out int h);
            return new Size((uint) w, (uint) h);
        }
    }

    public static Vector2 MouseDelta => _mouseDelta;

    public static bool MouseVisible
    {
        get => !SDL.GetWindowRelativeMouseMode(_sdlWindow);
        set => SDL.SetWindowRelativeMouseMode(_sdlWindow, !value);
    }

    public static bool IsKeyDown(Key key)
        => _keysDown.Contains(key);

    public static bool IsKeyPressed(Key key)
        => _keysPressed.Contains(key);

    public static void SetDemo(Demos.Demo demo)
    {
        _currentDemo = demo;
    }

    public static void LoadDemo(Demos.Demo demo)
    {
        _demoToSwitch = new LoadingScreen(demo);
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
        ImFontPtr font = ImGui.AddFont("Content/Roboto-Regular.ttf");

        string[] paths = Directory.GetFiles("Content/DemoImages");
        _backgroundTextures = new Texture[paths.Length];
        for (int i = 0; i < paths.Length; i++)
            _backgroundTextures[i] = new Texture(_renderer, paths[i]);

        _currentDemo = new WelcomeScreen();
        _currentDemo.Initialize();

        Stopwatch sw = Stopwatch.StartNew();
        
        _alive = true;
        while (_alive)
        {
            _keysPressed.Clear();
            _mouseDelta = Vector2.Zero;
            ImGuiIOPtr io = ImGui.GetIO();
            
            while (SDL.PollEvent(out SDL.Event winEvent))
            {
                switch ((SDL.EventType) winEvent.Type)
                {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        _alive = false;
                        break;
                    case SDL.EventType.WindowResized:
                        Renderer.Resize(WindowSize);
                        break;

                    case SDL.EventType.KeyDown:
                    {
                        _activityTimer = 0;
                        // Ignore the key repeat command
                        if (winEvent.Key.Repeat)
                            break;

                        _keysDown.Add(winEvent.Key.Key);
                        _keysPressed.Add(winEvent.Key.Key);
                        break;
                    }

                    case SDL.EventType.KeyUp:
                    {
                        _activityTimer = 0;
                        _keysDown.Remove(winEvent.Key.Key);
                        _keysPressed.Remove(winEvent.Key.Key);
                        break;
                    }

                    case SDL.EventType.MouseMotion:
                    {
                        _activityTimer = 0;
                        _mouseDelta += new Vector2(winEvent.Motion.XRel, winEvent.Motion.YRel);
                        io.AddMousePosEvent(winEvent.Motion.X, winEvent.Motion.Y);
                        break;
                    }

                    case SDL.EventType.MouseButtonDown:
                        _activityTimer = 0;
                        io.AddMouseButtonEvent((int) MouseButtonToImGui((MouseButton) winEvent.Button.Button), true);
                        break;
                    case SDL.EventType.MouseButtonUp:
                        _activityTimer = 0;
                        io.AddMouseButtonEvent((int) MouseButtonToImGui((MouseButton) winEvent.Button.Button), false);
                        break;
                    case SDL.EventType.MouseWheel:
                        _activityTimer = 0;
                        io.AddMouseWheelEvent(winEvent.Wheel.X, winEvent.Wheel.Y);
                        break;
                }
            }

            float dt = (float) sw.Elapsed.TotalSeconds;
            sw.Restart();

            Renderer.NewFrame();
            ImGui.NewFrame();
            
            if (_currentDemo is not (WelcomeScreen or LoadingScreen))
            {
                _activityTimer += dt;

                if (_activityTimer >= ActivityTimeout - 5)
                {
                    ImGui.DrawText(new Vector2(0, WindowSize.Height - 50), 48,
                        $"Returning to menu in {-(_activityTimer - ActivityTimeout):0.0}...", Color.Red);
                }
                
                if (_activityTimer >= ActivityTimeout)
                    LoadDemo(new WelcomeScreen());
            }

            if (_demoToSwitch != null)
            {
                _currentDemo.Dispose();
                _currentDemo = null!;
                GC.Collect();
                _currentDemo = _demoToSwitch;
                _currentDemo.Initialize();
                _demoToSwitch = null;
            }
            
            _currentDemo.Update(dt);
            _currentDemo.DisplayUI();
            _currentDemo.Draw();
            
            Renderer.Render();
        }
        
        Renderer.Dispose();
        SDL.DestroyWindow(_sdlWindow);
        SDL.Quit();
    }

    private static ImGuiMouseButton MouseButtonToImGui(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => ImGuiMouseButton.Left,
            MouseButton.Middle => ImGuiMouseButton.Middle,
            MouseButton.Right => ImGuiMouseButton.Right,
            _ => ImGuiMouseButton.Left // return left as a fallback. it'll do.
        };
    }
}