namespace Renderer.Math;

public struct Offset : IEquatable<Offset>
{
    public static Offset Zero => new Offset(0);
    
    public int X;

    public int Y;

    public Offset(int xy)
    {
        X = xy;
        Y = xy;
    }

    public Offset(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Offset other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Offset other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Offset left, Offset right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Offset left, Offset right)
    {
        return !left.Equals(right);
    }
}