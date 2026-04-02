using SDL3;

namespace Renderer.Utils;

internal static class ShaderUtils
{
    public static unsafe IntPtr LoadShader(IntPtr device, ShaderCross.ShaderStage stage, string name)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Shaders", $"{name}.hlsl");
        string? includeDir = Path.GetDirectoryName(fullPath);
        //SDL.GPUShaderFormat format = SDL.GetGPUShaderFormats(device);
        string hlsl = File.ReadAllText(fullPath);
        string entryPoint = stage switch
        {
            ShaderCross.ShaderStage.Vertex => "VSMain",
            ShaderCross.ShaderStage.Fragment => "PSMain",
            ShaderCross.ShaderStage.Compute => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };

        ShaderCross.HLSLInfo hlslInfo = new()
        {
            ShaderStage = stage,
            ManagedSource = hlsl,
            ManagedEntrypoint = entryPoint,
            ManagedIncludeDir = includeDir
        };
        nint spirv = ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out nuint size).Check("Compile shader from HLSL");

        var metadata = (ShaderCross.GraphicsShaderMetadata*) ShaderCross.ReflectGraphicsSPIRV(spirv, size, 0);
        ref var resourceInfo = ref metadata->ResourceInfo;

        ShaderCross.SPIRVInfo spirvInfo = new()
        {
            ShaderStage = stage,
            ByteCode = spirv,
            ByteCodeSize = size,
            ManagedEntrypoint = entryPoint
        };
        IntPtr shader = ShaderCross.CompileGraphicsShaderFromSPIRV(device, ref spirvInfo, ref resourceInfo, 0)
            .Check("Compile shader");

        return shader;
    }
}