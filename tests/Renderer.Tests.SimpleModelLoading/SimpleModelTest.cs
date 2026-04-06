using System.Drawing;
using System.Numerics;
using Renderer.Skyboxes;
using Renderer.Tests.Common;

namespace Renderer.Tests.SimpleModelLoading;

public class SimpleModelTest() : TestBase("Simple Model Test")
{
    private Skybox _skybox = null!;
    private Model _model = null!;
    private float _rotation;
    
    protected override void Load()
    {
        _skybox = new Skybox(Renderer, "Content/Skybox/right.jpg", "Content/Skybox/left.jpg", "Content/Skybox/top.jpg",
            "Content/Skybox/bottom.jpg", "Content/Skybox/front.jpg", "Content/Skybox/back.jpg");
        
        _model = new Model(Renderer, "Content/Models/WaterBottle.glb");
    }

    protected override void Loop(float dt)
    {
        _rotation += dt;
        if (_rotation >= float.Pi * 2 * 2)
            _rotation -= float.Pi * 2 * 2;
        
        _model.Draw(Renderer, Matrix4x4.CreateScale(5) * Matrix4x4.CreateFromYawPitchRoll(_rotation * 0.5f, _rotation, _rotation));

        Size size = Size;
        
        Camera camera = Camera.Perspective(new Vector3(0, 0f, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, (int) size.Width, (int) size.Height), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in camera);
    }

    public override void Dispose()
    {
        _model.Dispose();
        _skybox.Dispose();
        base.Dispose();
    }
}