using System.Runtime.InteropServices;
using Renderer.Math;

namespace Renderer.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneData
{
    public ShaderCamera Camera;
    public uint NumLights;
    public Size TargetSize;
}