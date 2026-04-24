using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Primitives;
using SDL3;

namespace Demo.Demos;

public class LightCasterDemo() : Demo("Light Casters")
{
    private Skybox _skybox = null!;
    
    private Material _material = null!;
    private Renderable _cube = null!;

    private Material _lightMaterial = null!;
    private Renderable _lightCube = null!;

    private Model _fox = null!;
    private Model _lamp = null!;

    private Light[] _lights = null!;
    private int _numLights;

    private float _value;
    private bool _useArcball;
    private bool _showLights;
    private float _renderScale;
    
    private Vector3 _cameraPos;
    private Vector2 _cameraRotation;
    
    public override void Initialize()
    {
        _skybox = new Skybox(Renderer, "Content/Skybox/Space1/right.png", "Content/Skybox/Space1/left.png", "Content/Skybox/Space1/top.png",
            "Content/Skybox/Space1/bottom.png", "Content/Skybox/Space1/front.png", "Content/Skybox/Space1/back.png");
        
        _material = new StandardMaterial(Renderer, new Texture(Renderer, "Content/PBR/metalgrid3_basecolor.png"))
        {
            Normal = new Texture(Renderer, "Content/PBR/metalgrid3_normal-dx.png"),
            Metallic = new Texture(Renderer, "Content/PBR/metalgrid3_metallic.png"),
            Roughness = new Texture(Renderer, "Content/PBR/metalgrid3_roughness.png"),
            Occlusion = new Texture(Renderer, "Content/PBR/metalgrid3_AO.png")
        };
        
        _lightMaterial = new UnlitMaterial(Renderer, Renderer.WhiteTexture);

        Cube cube = new Cube();
        _cube = new Renderable(Renderer, _material, cube.Vertices, cube.Indices);
        _lightCube = new Renderable(Renderer, _lightMaterial, cube.Vertices, cube.Indices);

        // Max 1024 lights
        _lights = new Light[1024];
        _numLights = 32;
        SetLights();

        _fox = new Model(Renderer, "Content/Models/Fox.glb");
        _lamp = new Model(Renderer, "Content/Models/WaterBottle.glb");

        _useArcball = true;
        _renderScale = 1;
    }

    public override void DisplayUI()
    {
        if (_useArcball && ImGui.BeginDemoSettingsWindow())
        {
            if (ImGui.SliderInt("Number of Lights", ref _numLights, 1, _lights.Length))
                SetLights();
            ImGui.SetItemTooltip("Increase/decrease the number of lights on-screen,\nand see how it affects performance.");
            
            if (ImGui.Button("Randomize Lights"))
                SetLights();
            ImGui.SetItemTooltip("Randomize the position of each light.");

            ImGui.Checkbox("Show Lights", ref _showLights);
            ImGui.SetItemTooltip("Show the light casters on screen.");

            ImGui.SliderFloat("Render Scale", ref _renderScale, 0.1f, 1.0f);
            ImGui.SetItemTooltip("Set how much of the screen is taken up by the image,\nand see how it affects performance.");

            if (ImGui.Button("Enter Free-look mode"))
            {
                _useArcball = false;
                DemoApp.MouseVisible = false;
            }
            ImGui.SetItemTooltip("Enter a free-look mode where you can use the mouse & keyboard\nto move the camera around freely.");

            ImGui.Separator();
            
            if (ImGui.Button("Exit Demo"))
                DemoApp.LoadDemo(new WelcomeScreen());
            ImGui.SetItemTooltip("Exit the demo and return to the main menu.");
            
            ImGui.End();
        }
        else if (ImGui.BeginDemoSettingsWindow())
        {
            ImGui.PushFont(ImFontPtr.Null, 48);
            ImGui.Text("Free-look Mode");
            ImGui.PopFont();
            
            ImGui.Text("Press C to exit.");
            
            ImGui.SeparatorText("Controls");
            ImGui.Text("W, A, S, D: Movement");
            ImGui.Text("Mouse: Look Around");
            ImGui.Text("Shift: Speed up");
            
            ImGui.End();
        }
        
        base.DisplayUI();
    }

    public override void Update(float dt)
    {
        if (DemoApp.IsKeyPressed(Key.C))
        {
            _useArcball = !_useArcball;
            DemoApp.MouseVisible = _useArcball;
        }

        if (DemoApp.IsKeyPressed(Key.R))
            SetLights();

        if (_useArcball)
        {
            _value += dt * 0.15f;
            if (_value >= float.Pi * 2)
                _value -= float.Pi * 2;
            
            // Arcball camera
            const float distance = 12;
            float x = float.Sin(_value) * distance;
            float z = float.Cos(_value) * distance;
            _cameraPos = new Vector3(x, 4, z);
            _cameraRotation = new Vector2(_value, float.DegreesToRadians(-40));
        }
        else
        {
            const float mouseSensitivity = 0.01f;
            Vector2 mouseDelta = DemoApp.MouseDelta;
            _cameraRotation.X -= mouseDelta.X * mouseSensitivity;
            _cameraRotation.Y -= mouseDelta.Y * mouseSensitivity;
            // Clamp camera rotation to 180 degree range to prevent gimbal lock
            _cameraRotation.Y = float.Clamp(_cameraRotation.Y, -float.Pi / 2, float.Pi / 2);
            
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(_cameraRotation.X, _cameraRotation.Y, 0);
            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, rotation);
            Vector3 right = Vector3.Transform(Vector3.UnitX, rotation);
            Vector3 up = Vector3.Cross(right, forward);

            float cameraSpeed = (DemoApp.IsKeyDown(Key.LShift) ? 20 : 10) * dt;

            if (DemoApp.IsKeyDown(Key.W))
                _cameraPos += forward * cameraSpeed;
            if (DemoApp.IsKeyDown(Key.S))
                _cameraPos -= forward * cameraSpeed;
            if (DemoApp.IsKeyDown(Key.D))
                _cameraPos += right * cameraSpeed;
            if (DemoApp.IsKeyDown(Key.A))
                _cameraPos -= right * cameraSpeed;
            if (DemoApp.IsKeyDown(Key.Space))
                _cameraPos += up * cameraSpeed;
            if (DemoApp.IsKeyDown(Key.LCtrl))
                _cameraPos -= up * cameraSpeed;
        }
        
        base.Update(dt);
    }

    public override void Draw()
    {
        Renderer.Draw(_cube, Matrix4x4.CreateScale(20, 0.1f, 20));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(-5, 0.5f, 3));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(6, 0.5f, -6));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(-2, 0.5f, -2));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(1, 0.5f, 8));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(9, 0.5f, 4));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(-8, 0.5f, -3));
        Renderer.Draw(_cube, Matrix4x4.CreateTranslation(-6, 0.5f, -8));
        
        _fox.Draw(Renderer, Matrix4x4.CreateScale(0.02f));
        _lamp.Draw(Renderer, Matrix4x4.CreateScale(10) * Matrix4x4.CreateTranslation(2, 1.3f, 6));

        if (_showLights)
        {
            for (int i = 0; i < _numLights; i++)
                Renderer.Draw(_lightCube, Matrix4x4.CreateScale(0.2f) * Matrix4x4.CreateTranslation(_lights[i].Position));
        }

        Size windowSize = DemoApp.WindowSize;
        Size renderSize = new()
        {
            Width = (uint) (windowSize.Width * _renderScale),
            Height = (uint) (windowSize.Height * _renderScale)
        };

        Offset offset = new Offset((int) (windowSize.Width / 2 - renderSize.Width / 2),
            (int) (windowSize.Height / 2 - renderSize.Height / 2));
        
        Renderer.AddCamera(Camera.Perspective(_cameraPos,
            Quaternion.CreateFromYawPitchRoll(_cameraRotation.X, _cameraRotation.Y, 0), float.DegreesToRadians(75),
            new Rectangle(offset, renderSize), 0.1f, 100f, _skybox));
        
        for (int i = 0; i < _numLights; i++)
            Renderer.AddLight(_lights[i]);
        
        base.Draw();
    }

    public override void Dispose()
    {
        _cube.Dispose();
        _material.ReleaseAllTexturesAndDispose();
        _skybox.Dispose();
    }

    private void SetLights()
    {
        Random random = new Random();
        for (int i = 0; i < _numLights; i++)
        {
            _lights[i] = Light.Point(new Vector3
            {
                X = float.Lerp(-8, 8, random.NextSingle()),
                Y = 1,
                Z = float.Lerp(-8, 8, random.NextSingle())
            }, Color.Normalize(new Color
            {
                R = random.NextSingle(),
                G = random.NextSingle(),
                B = random.NextSingle(),
                A = 1
            }), 800, 20);
        }
    }
}