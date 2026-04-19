using System.Numerics;

namespace Renderer.Structs;

internal struct ShaderLight
{
    public LightType Type;
    public Vector4 Position;
    public Color Color;

    public enum LightType
    {
        Point = 1
    }
}