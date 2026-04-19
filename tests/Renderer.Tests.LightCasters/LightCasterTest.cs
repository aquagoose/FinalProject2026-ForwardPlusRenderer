using System.Numerics;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Primitives;
using Renderer.Skyboxes;
using Renderer.Tests.Common;

namespace Renderer.Tests.LightCasters;

public class LightCasterTest() : TestBase("Light Caster Test")
{
    private Skybox _skybox = null!;
    private Material _material = null!;
    private Renderable _cube = null!;

    private Model _fox;
    private Model _lamp;

    private Light[] _lights = new Light[32];

    private float _azimuth;
    
    protected override void Load()
    {
        _skybox = new Skybox(Renderer, "Content/Skybox/right.jpg", "Content/Skybox/left.jpg", "Content/Skybox/top.jpg",
            "Content/Skybox/bottom.jpg", "Content/Skybox/front.jpg", "Content/Skybox/back.jpg");
        
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

        _fox = new Model(Renderer, "Content/Models/Fox.glb");
        _lamp = new Model(Renderer, "Content/Models/WaterBottle.glb");
    }

    protected override void Loop(float dt)
    {
        _azimuth += dt * 0.25f;
        if (_azimuth >= float.Pi * 2)
            _azimuth -= float.Pi * 2;
        
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

        // Arcball camera
        const float distance = 12;
        float x = float.Sin(_azimuth) * distance;
        float z = float.Cos(_azimuth) * distance;
        Renderer.AddCamera(Camera.Perspective(new Vector3(x, 4, z),
            Quaternion.CreateFromYawPitchRoll(_azimuth, float.DegreesToRadians(-40), 0), float.DegreesToRadians(75),
            new Rectangle(Offset.Zero, Size), 0.1f, 100f, _skybox));
        
        foreach (Light light in _lights)
            Renderer.AddLight(light);
    }

    public override void Dispose()
    {
        _cube.Dispose();
        _material.ReleaseAllTexturesAndDispose();
        _skybox.Dispose();
        base.Dispose();
    }
}