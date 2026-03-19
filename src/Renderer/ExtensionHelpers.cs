namespace Renderer;

/// <summary>
/// Contains a series of extensions to make the API easier to use.
/// </summary>
public static class ExtensionHelpers
{
    extension(PixelFormat format)
    {
        /// <summary>
        /// Gets the number of bytes per pixel.
        /// </summary>
        public uint BytesPerPixel
        {
            get
            {
                return format switch
                {
                    PixelFormat.RGBA8 => 4,
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
            }
        }
    }
}