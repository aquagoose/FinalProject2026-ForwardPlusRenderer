using System.Runtime.InteropServices;

namespace Renderer;

/// <summary>
/// A floating point RGBA color, optimized for sending to the GPU.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Color
{
    /// <summary>
    /// The red component.
    /// </summary>
    public float R;

    /// <summary>
    /// The green component.
    /// </summary>
    public float G;

    /// <summary>
    /// The blue component.
    /// </summary>
    public float B;

    /// <summary>
    /// The alpha component.
    /// </summary>
    public float A;

    /// <summary>
    /// Create a <see cref="Color"/> from floating point red, green, blue, and alpha components.
    /// </summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="a">The alpha component.</param>
    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Create a <see cref="Color"/> from 8-bit red, green, blue, and alpha components.
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public Color(byte r, byte g, byte b, byte a = byte.MaxValue)
    {
        R = r / (float) byte.MaxValue;
        G = g / (float) byte.MaxValue;
        B = b / (float) byte.MaxValue;
        A = a / (float) byte.MaxValue;
    }
}