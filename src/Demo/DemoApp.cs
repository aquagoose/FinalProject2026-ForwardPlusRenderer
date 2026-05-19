global using Key = SDL3.SDL.Keycode;
global using MouseButton = SDL3.SDL.MouseButtonFlags;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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
    private static float _fpsUpdateTimer;
    private static uint _fpsCounter;
    private static uint _currentFps;
    private static float _currentDeltaTime;
    private static SDL.DialogFileCallback _fileCallback;

    private static HashSet<Key> _keysDown = [];
    private static HashSet<Key> _keysPressed = [];
    private static Vector2 _mouseDelta;

    private static Renderer.Renderer _renderer = null!;
    private static Demos.Demo _currentDemo = null!;
    private static Demos.Demo? _demoToSwitch = null;

    public static bool EnableIdleTimer = true;

    private static Texture[] _backgroundTextures = null!;

    public static Renderer.Renderer Renderer => _renderer;

    public static Texture[] BackgroundTextures => _backgroundTextures;

    public static uint FPS => _currentFps;

    public static float DeltaTime => _currentDeltaTime;

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

    static DemoApp()
    {
        _fileCallback = FileCallback;
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

        float scale = SDL.GetWindowDisplayScale(_sdlWindow);
        float pixelDensity = SDL.GetWindowPixelDensity(_sdlWindow);

        _renderer = new Renderer.Renderer(_sdlWindow);
        ImFontPtr font = ImGui.AddFont("Content/Roboto-Regular.ttf");
        ImGuiStylePtr style = ImGui.GetStyle();
        style.ScaleAllSizes(scale);
        style.FontScaleDpi = scale;

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
                    case SDL.EventType.WindowDisplayScaleChanged:
                        scale = SDL.GetWindowDisplayScale(_sdlWindow);
                        pixelDensity = SDL.GetWindowPixelDensity(_sdlWindow);
                        Renderer.Resize(WindowSize);

                        style = ImGui.GetStyle();
                        style.MainScale = scale;
                        style.FontScaleDpi = scale;
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
                            ref readonly SDL.MouseMotionEvent motion = ref winEvent.Motion;

                            _mouseDelta += new Vector2(motion.XRel, motion.YRel);
                            io.AddMousePosEvent(motion.X * pixelDensity, motion.Y * pixelDensity);
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

            _fpsUpdateTimer += dt;
            _fpsCounter++;
            _currentDeltaTime = dt;
            if (_fpsUpdateTimer >= 1.0f)
            {
                _fpsUpdateTimer -= 1.0f;
                _currentFps = _fpsCounter;
                _fpsCounter = 0;
            }

            Renderer.NewFrame();
            ImGui.NewFrame();

            if (EnableIdleTimer && _currentDemo is not LoadingScreen)
            {
                _activityTimer += dt;

                if (_activityTimer >= ActivityTimeout)
                {
                    _activityTimer = 0;
                    if (_currentDemo is WelcomeScreen)
                    {
                        // Weight it towards the Light Caster demo because it's better
                        int random = Random.Shared.Next(3);
                        if (random == 1)
                            LoadDemo(new TransparencyDemo());
                        else
                            LoadDemo(new LightCasterDemo());
                    }
                    else
                        LoadDemo(new WelcomeScreen());
                }
                else if (_activityTimer >= ActivityTimeout - 5 && _currentDemo is not WelcomeScreen)
                {
                    string text = $"Returning to menu in {-(_activityTimer - ActivityTimeout):0.0}...";
                    const uint size = 48;
                    Size textSize = ImGui.MeasureText(size, text);

                    ImGui.DrawText(new Vector2(0, WindowSize.Height - textSize.Height), size, text, Color.White);
                }
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

    public static void ShowSaveFilePopup(string fileTypes, string fileTypeName, string textToWrite)
    {
        SDL.DialogFileFilter filter = new(fileTypeName, fileTypes);
        GCHandle handle = GCHandle.Alloc(textToWrite, GCHandleType.Pinned);
        string? location = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
        if (string.IsNullOrWhiteSpace(location))
            location = null;
        
        SDL.ShowSaveFileDialog(_fileCallback, (nint) handle, _sdlWindow, [filter], 1, location);
    }

    private static unsafe void FileCallback(IntPtr userdata, IntPtr filelist, int filter)
    {
        GCHandle handle = (GCHandle) userdata;
        
        sbyte** files = (sbyte**) filelist;
        while (*files != null)
        {
            string filePath = new string(*files);
            string textToWrite = (string) handle.Target;
            File.WriteAllText(filePath, textToWrite);
            files++;
        }
        
        handle.Free();
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
