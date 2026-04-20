using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;

namespace Demo.Demos;

public class WelcomeScreen() : Demo(null)
{
    private uint _currentBackground;
    private float _timer;
    private float _fade;
    
    public override void Initialize()
    {
        DemoApp.MouseVisible = true;
    }

    public override void DisplayUI()
    {
        ImGui.PushFont(ImFontPtr.Null, 24);
        
        if (ImGui.Begin("Welcome",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushFont(ImFontPtr.Null, 48);
            ImGui.TextUnformatted("Welcome!");
            ImGui.PopFont();
            
            ImGui.TextUnformatted("Please select a demo.");
            
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 20));
            if (ImGui.Button("Light Casters"))
                DemoApp.LoadDemo(new LightCasterDemo());
            //if (ImGui.Button("Sponza"))
            //    DemoApp.LoadDemo(new SponzaDemo());
            ImGui.PopStyleVar();
            
            ImGui.End();
        }
        
        ImGui.PopFont();
    }

    public override void Update(float dt)
    {
        _timer += dt;
        if (_timer >= 5)
        {
            _timer -= 5;
            _currentBackground = (uint) ((_currentBackground + 1) % DemoApp.BackgroundTextures.Length);
        }
    }

    public override void Draw()
    {
        Texture texture = DemoApp.BackgroundTextures[_currentBackground];
        
        ImGui.DrawImage(texture, Vector2.Zero, DemoApp.WindowSize);
        ImGui.DrawRectangle(Vector2.Zero, new Size(1200, DemoApp.WindowSize.Height), Color.Black, Color.Transparent,
            Color.Black, Color.Transparent);
    }
}