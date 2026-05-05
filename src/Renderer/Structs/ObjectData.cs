using System.Numerics;

namespace Renderer.Structs;

/// <summary>
/// Per-object data.
/// </summary>
internal struct ObjectData
{
    public Matrix4x4 WorldMatrix;
    public Matrix4x4 NormalMatrix;

    public ObjectData(Matrix4x4 worldMatrix, Matrix4x4 normalMatrix)
    {
        WorldMatrix = worldMatrix;
        NormalMatrix = normalMatrix;
    }
}