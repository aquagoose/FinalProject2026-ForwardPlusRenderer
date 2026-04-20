using Hexa.NET.ImGui;

namespace Demo.Demos;

public class WelcomeScreen() : Demo(null)
{
    public override void Update(float dt)
    {
        if (ImGui.Begin("Welcome",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushFont(ImFontPtr.Null, 32);
            ImGui.TextUnformatted("Welcome");
            ImGui.PopFont();
            
            ImGui.TextUnformatted("Please select a demo.");
            
            if (ImGui.Button("Light Casters"))
                DemoApp.LoadDemo(new LightCasterDemo());
            
            ImGui.End();
        }
    }
}