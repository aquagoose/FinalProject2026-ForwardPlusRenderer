using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Math;

namespace Demo;

// Dear ImGui extensions.
public static class ImGuiE
{
    extension(ImGui)
    {
        public static unsafe void DrawText(Vector2 position, uint size, string text, Color color)
        {
            ImGui.GetForegroundDrawList().AddText(ImGui.GetIO().FontDefault, size, position, uint.MaxValue, text);
        }

        public static unsafe void DrawImage(Texture texture, Vector2 position, Size size, Color? tint = null)
        {
            uint packedTint = tint?.PackedValue ?? uint.MaxValue;
            
            ImGui.GetBackgroundDrawList().AddImage(new ImTextureRef(texId: texture.Handle), position,
                new Vector2(position.X + size.Width, position.Y + size.Height), packedTint);
        }

        public static void DrawRectangle(Vector2 position, Size size, Color color)
        {
            ImGui.GetBackgroundDrawList().AddRectFilled(position,
                new Vector2(position.X + size.Width, position.Y + size.Height), color.PackedValue);
        }

        public static void DrawRectangle(Vector2 position, Size size, Color topLeft, Color topRight, Color bottomLeft,
            Color bottomRight)
        {
            ImGui.GetBackgroundDrawList().AddRectFilledMultiColor(position,
                new Vector2(position.X + size.Width, position.Y + size.Height), topLeft.PackedValue,
                topRight.PackedValue, bottomRight.PackedValue, bottomLeft.PackedValue);
        }

        public static bool BeginDemoSettingsWindow()
        {
            return ImGui.Begin("Demo Settings", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        }

        public static unsafe ImFontPtr AddFont(string path)
        {
            ImFontAtlasPtr fonts = ImGui.GetIO().Fonts;
            
            ImFontConfig config = new()
            {
                MergeMode = (byte) (fonts.Fonts.Size > 0 ? 1 : 0),
                FontDataOwnedByAtlas = 1,
                RasterizerDensity = 1,
                RasterizerMultiply = 1,
                GlyphMaxAdvanceX = float.MaxValue
            };
            
            ImFontPtr font = fonts.AddFontFromFileTTF(path, 18, &config);
            return font;
        }
    }

    extension(Color color)
    {
        public uint PackedValue
        {
            get
            {
                byte r = (byte) (color.R * byte.MaxValue);
                byte g = (byte) (color.G * byte.MaxValue);
                byte b = (byte) (color.B * byte.MaxValue);
                byte a = (byte) (color.A * byte.MaxValue);

                return (uint) ((a << 24) | (b << 16) | (g << 8) | r);
            }
        }
    }
}