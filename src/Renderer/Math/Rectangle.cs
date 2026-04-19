namespace Renderer.Math;

public struct Rectangle : IEquatable<Rectangle>
{
    public Offset Offset;

    public Size Size;

    public Rectangle(Offset offset, Size size)
    {
        Offset = offset;
        Size = size;
    }

    public Rectangle(int x, int y, uint width, uint height)
    {
        Offset = new Offset(x, y);
        Size = new Size(width, height);
    }

    public int X
    {
        get => Offset.X;
        set => Offset.X = value;
    }

    public int Y
    {
        get => Offset.Y;
        set => Offset.Y = value;
    }

    public uint Width
    {
        get => Size.Width;
        set => Size.Width = value;
    }

    public uint Height
    {
        get => Size.Height;
        set => Size.Height = value;
    }


    public bool Equals(Rectangle other)
    {
        return Offset.Equals(other.Offset) && Size.Equals(other.Size);
    }

    public override bool Equals(object? obj)
    {
        return obj is Rectangle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Size);
    }

    public static bool operator ==(Rectangle left, Rectangle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rectangle left, Rectangle right)
    {
        return !left.Equals(right);
    }
}