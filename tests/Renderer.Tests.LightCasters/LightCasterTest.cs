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
    private Renderable _renderable = null!;

    private Light[] _lights = new Light[32];

    private float _value;
    
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
        _renderable = new Renderable(Renderer, _material, cube.Vertices, cube.Indices);

        Random random = new Random();
        for (int i = 0; i < _lights.Length; i++)
        {
            _lights[i] = Light.Point(new Vector3
            {
                X = float.Lerp(-8, 8, random.NextSingle()),
                Y = 1,
                Z = float.Lerp(-8, 8, random.NextSingle())
            }, new Color
            {
                R = random.NextSingle(),
                G = random.NextSingle(),
                B = random.NextSingle(),
                A = 1
            }, 800, 20);
        }
    }

    protected override void Loop(float dt)
    {
        _value += dt * 0.5f;
        if (_value >= float.Pi * 2)
            _value -= float.Pi * 2;
        
        Renderer.Draw(_renderable, Matrix4x4.CreateScale(20, 0.1f, 20));

        // Arcball camera
        const float distance = 12;
        float x = float.Sin(_value) * distance;
        float z = float.Cos(_value) * distance;
        Renderer.AddCamera(Camera.Perspective(new Vector3(x, 4, z),
            Quaternion.CreateFromYawPitchRoll(_value, float.DegreesToRadians(-40), 0), float.DegreesToRadians(75),
            new Rectangle(Offset.Zero, Size), 0.1f, 100f, _skybox));
        
        foreach (Light light in _lights)
            Renderer.AddLight(light);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.ReleaseAllTexturesAndDispose();
        _skybox.Dispose();
        base.Dispose();
    }
}