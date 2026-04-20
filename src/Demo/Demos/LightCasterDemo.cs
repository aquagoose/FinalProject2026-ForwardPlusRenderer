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

    private Model _fox;
    private Model _lamp;

    private Light[] _lights = null!;

    private float _value;
    private bool _useArcball;
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
        //_material = new StandardMaterial(Renderer, Renderer.WhiteTexture);

        Cube cube = new Cube();
        _cube = new Renderable(Renderer, _material, cube.Vertices, cube.Indices);

        SetLights(32);

        _fox = new Model(Renderer, "Content/Models/Fox.glb");
        _lamp = new Model(Renderer, "Content/Models/WaterBottle.glb");

        _useArcball = true;
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        
        if (DemoApp.IsKeyPressed(Key.C))
        {
            _useArcball = !_useArcball;
            DemoApp.MouseVisible = _useArcball;
        }
        
        if (DemoApp.IsKeyPressed(Key.R))
            SetLights(_lights.Length);

        if (_useArcball)
        {
            _value += dt * 0.25f;
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


        Renderer.AddCamera(Camera.Perspective(_cameraPos,
            Quaternion.CreateFromYawPitchRoll(_cameraRotation.X, _cameraRotation.Y, 0), float.DegreesToRadians(75),
            new Rectangle(Offset.Zero, DemoApp.WindowSize), 0.1f, 100f, _skybox));
        
        foreach (Light light in _lights)
            Renderer.AddLight(light);
    }

    public override void Dispose()
    {
        _cube.Dispose();
        _material.ReleaseAllTexturesAndDispose();
        _skybox.Dispose();
    }

    private void SetLights(int numLights)
    {
        _lights = new Light[numLights];
        
        Random random = new Random();
        for (int i = 0; i < _lights.Length; i++)
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