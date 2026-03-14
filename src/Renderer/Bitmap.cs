using StbImageSharp;

namespace Renderer;

public class Bitmap
{
    public byte[] Data;

    public Size Size;

    public PixelFormat Format;
    
    public Bitmap(string path)
    {
        using FileStream stream = File.OpenRead(path);
        ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        Data = result.Data;
        Size = new Size((uint) result.Width, (uint) result.Height);
        Format = PixelFormat.RGBA8;
    }
}