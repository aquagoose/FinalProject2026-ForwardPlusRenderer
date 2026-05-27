using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;

namespace Demo.Demos;

public abstract class Demo(string? name) : IDisposable
{
    protected Renderer.Renderer Renderer => DemoApp.Renderer;
    
    public virtual void Initialize() { }

    public virtual void DisplayUI()
    {
        if (name != null)
            ImGui.DrawText(Vector2.Zero, 72, name, Color.White);
    }
    
    public virtual void Update(float dt)
    {
        if (DemoApp.IsKeyPressed(Key.Escape))
            DemoApp.LoadDemo(new WelcomeScreen());
    }

    public virtual void Draw()
    {
        Size renderSize = Renderer.Size;
        string text = $"FPS: {DemoApp.FPS}\nΔt: {(DemoApp.DeltaTime * 1000):00.0}ms";
        const uint size = 72;
        Size textSize = ImGui.MeasureText(size, text);
        
        ImGui.DrawText(new Vector2(renderSize.Width - textSize.Width - 5, 0), size, text, Color.White);
    }

    public virtual void Dispose() { }
}