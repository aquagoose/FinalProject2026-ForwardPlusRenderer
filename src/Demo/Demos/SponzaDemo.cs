using System.Numerics;
using Renderer;
using Renderer.Math;

namespace Demo.Demos;

public class SponzaDemo() : Demo("Sponza")
{
    private Model _model = null!;
    
    public override void Initialize()
    {
        _model = new Model(Renderer, @"C:\Users\aqua\Documents\Untitled.glb");
    }

    public override void Draw()
    {
        _model.Draw(Renderer, Matrix4x4.Identity);

        Renderer.AddCamera(Camera.Perspective(Vector3.Zero, Quaternion.Identity, float.DegreesToRadians(75),
            new Rectangle(Offset.Zero, DemoApp.WindowSize), 0.1f, 1000f));
    }
}