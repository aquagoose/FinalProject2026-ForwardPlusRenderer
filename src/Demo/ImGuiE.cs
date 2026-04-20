using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;

namespace Demo;

// Dear ImGui extensions.
public static class ImGuiE
{
    extension(ImGui)
    {
        public static unsafe void Text(Vector2 position, uint size, string text, Color color)
        {
            ImGui.GetForegroundDrawList().AddText(ImGui.GetIO().FontDefault, size, position, uint.MaxValue, text);
        }

        public static unsafe ImFontPtr AddFont(string path)
        {
            ImFontAtlasPtr fonts = ImGui.GetIO().Fonts;
            
            ImFontConfig config = new()
            {
                MergeMode = (byte) (fonts.Fonts.Size > 1 ? 1 : 0),
                FontDataOwnedByAtlas = 1,
                RasterizerDensity = 1,
                RasterizerMultiply = 1,
                GlyphMaxAdvanceX = float.MaxValue
            };
            
            ImFontPtr font = fonts.AddFontFromFileTTF(path, 12, &config);
            return font;
        }
    }
}