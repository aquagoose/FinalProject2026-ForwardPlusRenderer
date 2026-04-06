using StbImageSharp;

namespace Renderer;

/// <summary>
/// Represents raw image data.
/// </summary>
public class Bitmap
{
    /// <summary>
    /// The image data array.
    /// </summary>
    public readonly byte[] Data;

    /// <summary>
    /// The size in pixels.
    /// </summary>
    public readonly Size Size;

    /// <summary>
    /// The <see cref="PixelFormat"/>.
    /// </summary>
    public readonly PixelFormat Format;
    
    /// <summary>
    /// Load a <see cref="Bitmap"/> from a file path.
    /// </summary>
    /// <param name="path"></param>
    public Bitmap(string path)
    {
        using FileStream stream = File.OpenRead(path);
        ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        Data = result.Data;
        Size = new Size((uint) result.Width, (uint) result.Height);
        Format = PixelFormat.RGBA8;
    }

    public Bitmap(byte[] imageData)
    {
        ImageResult result = ImageResult.FromMemory(imageData, ColorComponents.RedGreenBlueAlpha);
        Data = result.Data;
        Size = new Size((uint) result.Width, (uint) result.Height);
        Format = PixelFormat.RGBA8;
    }
}