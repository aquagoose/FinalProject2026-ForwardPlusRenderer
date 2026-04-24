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
        //ImGui.DrawImage(DemoApp.BackgroundTextures[0], Vector2.Zero, windowSize);

        const string loadingText = "Loading...";
        const uint size = 120;
        Size textSize = ImGui.MeasureText(size, loadingText);

        uint height = textSize.Height + 200;
        ImGui.DrawRectangle(new Vector2(0, windowSize.Height - height),
            new Size(windowSize.Width, height), Color.Transparent, Color.Transparent, Color.Black,
            Color.Black);
        ImGui.DrawText(new Vector2(10, windowSize.Height - textSize.Height), size, "Loading...", Color.White);
    }
}