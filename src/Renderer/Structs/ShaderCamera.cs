using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct ShaderCamera
{
    public Matrix4x4 Projection;

    public Matrix4x4 View;

    public Vector4 Position;

    public ShaderCamera(Matrix4x4 projection, Matrix4x4 view, Vector4 position)
    {
        Projection = projection;
        View = view;
        Position = position;
    }
}