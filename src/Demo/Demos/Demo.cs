using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;

namespace Demo.Demos;

public abstract class Demo(string name) : IDisposable
{
    protected Renderer.Renderer Renderer => DemoApp.Renderer;
    
    public virtual void Initialize() { }

    public virtual void Update(float dt)
    {
        ImGui.Text(Vector2.Zero, 36, name, Color.White);
    }

    public virtual void Draw() { }

    public virtual void Dispose() { }
}