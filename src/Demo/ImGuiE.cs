using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;

namespace Demo;

// Dear ImGui extensions.
public static class ImGuiE
{
    extension(ImGui)
    {
        public static void Text(Vector2 position, uint size, string text, Color color)
        {
            ImGui.GetForegroundDrawList().AddText(position, uint.MaxValue, text);
        }
    }
}