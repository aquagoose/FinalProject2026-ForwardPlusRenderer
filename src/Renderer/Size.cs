namespace Renderer;

public struct Size
{
    public uint Width;

    public uint Height;

    public Size(uint wh)
    {
        Width = wh;
        Height = wh;
    }

    public Size(uint width, uint height)
    {
        Width = width;
        Height = height;
    }
}