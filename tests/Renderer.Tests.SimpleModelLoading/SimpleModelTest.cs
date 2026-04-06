using System.Drawing;
using System.Numerics;
using Renderer.Tests.Common;

namespace Renderer.Tests.SimpleModelLoading;

public class SimpleModelTest() : TestBase("Simple Model Test")
{
    private Model _model = null!;
    private float _rotation;
    
    protected override void Load()
    {
        _model = new Model(Renderer, "Content/Models/Fox.glb");
    }

    protected override void Loop(float dt)
    {
        _rotation += dt;
        if (_rotation >= float.Pi * 2)
            _rotation -= float.Pi * 2;
        
        _model.Draw(Renderer, Matrix4x4.CreateScale(0.01f) * Matrix4x4.CreateRotationY(_rotation));

        Size size = Size;
        
        Camera camera = Camera.Perspective(new Vector3(0, 0.5f, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, (int) size.Width, (int) size.Height), 0.1f, 100f);
        Renderer.AddCamera(in camera);
    }
}