using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct ShaderLight
{
    public Vector3 Position;
    public LightType Type;
    public Color Color;

    public enum LightType
    {
        Point = 1
    }
}