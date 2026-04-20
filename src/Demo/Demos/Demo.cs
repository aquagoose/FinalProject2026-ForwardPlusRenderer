using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;

namespace Demo.Demos;

public abstract class Demo(string? name) : IDisposable
{
    protected Renderer.Renderer Renderer => DemoApp.Renderer;
    
    public virtual void Initialize() { }

    public virtual void DisplayUI()
    {
        if (name != null)
            ImGui.DrawText(Vector2.Zero, 36, name, Color.White);
    }
    
    public virtual void Update(float dt)
    {
        if (DemoApp.IsKeyPressed(Key.Escape))
            DemoApp.LoadDemo(new WelcomeScreen());
    }

    public virtual void Draw() { }

    public virtual void Dispose() { }
}