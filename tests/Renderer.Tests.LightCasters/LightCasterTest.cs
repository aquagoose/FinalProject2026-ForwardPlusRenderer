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

        Cube cube = new Cube();
        _renderable = new Renderable(Renderer, _material, cube.Vertices, cube.Indices);
    }

    protected override void Loop(float dt)
    {
        Renderer.Draw(_renderable, Matrix4x4.CreateScale(20, 0.1f, 20));
        
        Renderer.AddCamera(Camera.Perspective(new Vector3(0, 4, 12), Quaternion.CreateFromYawPitchRoll(0, float.DegreesToRadians(-35), 0), float.DegreesToRadians(75),
            new Rectangle(Offset.Zero, Size), 0.1f, 100f, _skybox));
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.ReleaseAllTexturesAndDispose();
        _skybox.Dispose();
        base.Dispose();
    }
}