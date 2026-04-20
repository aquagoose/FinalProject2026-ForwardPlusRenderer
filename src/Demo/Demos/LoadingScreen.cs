using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;

namespace Demo.Demos;

public class LoadingScreen(Demo demoToLoad) : Demo(null)
{
    private bool _hasDrawn;
    
    public override void Update(float dt)
    {
        if (!_hasDrawn)
            return;
        
        demoToLoad.Initialize();
        DemoApp.SetDemo(demoToLoad);
    }

    public override void Draw()
    {
        _hasDrawn = true;
        Size windowSize = DemoApp.WindowSize;
        
        // TODO: Don't use DemoApp.WindowSize for the demo images!
        ImGui.DrawImage(DemoApp.BackgroundTextures[Random.Shared.Next(DemoApp.BackgroundTextures.Length)], Vector2.Zero, windowSize);
        
        ImGui.DrawRectangle(new Vector2(0, windowSize.Height - 200), new Size(windowSize.Width, 200), Color.Black with { A = 0.5f });
        ImGui.DrawText(new Vector2(10, windowSize.Height - 200), 120, "Loading...", Color.White);
    }
}