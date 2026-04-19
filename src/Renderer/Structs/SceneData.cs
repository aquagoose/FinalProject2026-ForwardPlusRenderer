using System.Runtime.InteropServices;

namespace Renderer.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneData
{
    public ShaderCamera Camera;
    public uint NumLights;
}