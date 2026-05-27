using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;

namespace Demo.Demos;

public class WelcomeScreen() : Demo(null)
{
    private const float TimeOnScreen = 5;
    private const float TransitionTime = 1;
    
    private uint _currentBackground;
    private uint _nextBackground;
    private float _timer;
    private float _fade;
    
    public override void Initialize()
    {
        DemoApp.MouseVisible = true;
    }

    public override void DisplayUI()
    {
        ImGui.PushFont(ImFontPtr.Null, 48);
        
        if (ImGui.Begin("Welcome",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushFont(ImFontPtr.Null, 96);
            ImGui.TextUnformatted("Welcome!");
            ImGui.PopFont();
            
            ImGui.TextUnformatted("Please select a demo.");
            
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 20));
            if (ImGui.Button("Light Casters"))
                DemoApp.LoadDemo(new LightCasterDemo());
            if (ImGui.Button("Transparency"))
                DemoApp.LoadDemo(new TransparencyDemo());
            ImGui.PopStyleVar();
            
            ImGui.End();
        }
        
        ImGui.PopFont();
    }

    public override void Update(float dt)
    {
        _timer += dt;
        if (_timer >= TimeOnScreen)
        {
            _fade = (_timer - TimeOnScreen) / TransitionTime;
            _nextBackground = (uint) ((_currentBackground + 1) % DemoApp.BackgroundTextures.Length);

            if (_fade >= 1.0f)
            {
                _timer -= TimeOnScreen + TransitionTime;
                _fade = 0;
                _currentBackground = _nextBackground;
            }
        }
    }

    public override void Draw()
    {
        Texture texture = DemoApp.BackgroundTextures[_currentBackground];
        
        ImGui.DrawImage(texture, Vector2.Zero, DemoApp.WindowSize);
        if (_fade > 0)
        {
            Texture fadeTexture = DemoApp.BackgroundTextures[_nextBackground];
            ImGui.DrawImage(fadeTexture, Vector2.Zero, DemoApp.WindowSize, Color.White with { A = _fade });
        }
        
        ImGui.DrawRectangle(Vector2.Zero, new Size(1200, DemoApp.WindowSize.Height), Color.Black, Color.Transparent,
            Color.Black, Color.Transparent);
    }
}