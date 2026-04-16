using System.Runtime.InteropServices;
using SDL3;

namespace Renderer.Utils;

public static class ShaderUtils
{
    public static unsafe IntPtr CompileShader(ShaderCross.ShaderStage stage, string path, string entryPoint, out nuint size)
    {
        string? includeDir = Path.GetDirectoryName(path);
        //SDL.GPUShaderFormat format = SDL.GetGPUShaderFormats(device);
        string hlsl = File.ReadAllText(path);

        ShaderCross.HLSLInfo hlslInfo = new()
        {
            ShaderStage = stage,
            ManagedSource = hlsl,
            ManagedEntrypoint = entryPoint,
            ManagedIncludeDir = includeDir
        };
        nint spirv = ShaderCross.CompileSPIRVFromHLSL(ref hlslInfo, out size).Check("Compile shader from HLSL");
        return spirv;
    }
    
    public static unsafe IntPtr LoadShader(IntPtr device, ShaderCross.ShaderStage stage, string name)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Shaders", $"{name}.hlsl");
        
        string entryPoint = stage switch
        {
            ShaderCross.ShaderStage.Vertex => "VSMain",
            ShaderCross.ShaderStage.Fragment => "PSMain",
            ShaderCross.ShaderStage.Compute => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };
        
        IntPtr spirv = CompileShader(stage, fullPath, entryPoint, out nuint size);

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

    // SDL3-CS's ShaderCross bindings have the incorrect signature for this function, so I'm redefining it here with the
    // correct signature.
    [DllImport("SDL3_shadercross", EntryPoint = "SDL_ShaderCross_CompileComputePipelineFromSPIRV")]
    public static extern IntPtr CompileComputePipelineFromSPIRV(IntPtr device, in ShaderCross.SPIRVInfo spirvInfo,
        in ShaderCross.ComputePipelineMetadata metadata, uint props);
}