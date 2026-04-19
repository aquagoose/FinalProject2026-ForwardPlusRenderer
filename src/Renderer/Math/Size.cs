namespace Renderer.Math;

/// <summary>
/// Represents a 2D pixel size.
/// </summary>
public struct Size : IEquatable<Size>
{
    /// <summary>
    /// The width.
    /// </summary>
    public uint Width;

    /// <summary>
    /// The height.
    /// </summary>
    public uint Height;

    /// <summary>
    /// Create a size with a single width and height value.
    /// </summary>
    /// <param name="wh">The width and height.</param>
    public Size(uint wh)
    {
        Width = wh;
        Height = wh;
    }

    /// <summary>
    /// Create a size with a width and height.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    /// <inheritdoc />
    public bool Equals(Size other)
    {
        return Width == other.Width && Height == other.Height;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Size other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <summary>
    /// Compares if the two sizes are equal.
    /// </summary>
    /// <param name="left">The left hand side.</param>
    /// <param name="right">The right hand side.</param>
    /// <returns><see langword="true"/>, if the sizes are equal.</returns>
    public static bool operator ==(Size left, Size right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares if the two sizes are not equal.
    /// </summary>
    /// <param name="left">The left hand side.</param>
    /// <param name="right">The right hand side.</param>
    /// <returns><see langword="true"/>, if the sizes are not equal.</returns>
    public static bool operator !=(Size left, Size right)
    {
        return !left.Equals(right);
    }
}