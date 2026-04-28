using System.Runtime.InteropServices;
using SDL3;

namespace Renderer.Utils;

public static class ShaderUtils
{
    /// <summary>
    /// Compile an HLSL shader from the given path to SPIR-V.
    /// </summary>
    /// <param name="stage">The <see cref="ShaderCross.ShaderStage"/> to compile.</param>
    /// <param name="path">The full file path to the HLSL file.</param>
    /// <param name="entryPoint">The entry point of the shader.</param>
    /// <param name="size">The SPIR-V size in bytes.</param>
    /// <returns>A native array of SPIR-V with <paramref name="size"/> bytes of data.</returns>
    /// <remarks>Unless you intend to compile shaders separately, you probably want <see cref="LoadShader"/> instead.</remarks>
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
    
    /// <summary>
    /// Compile and load an HLSL shader.
    /// </summary>
    /// <param name="device">The SDL GPU device associated with this shader.</param>
    /// <param name="stage">The <see cref="ShaderCross.ShaderStage"/> to compile.</param>
    /// <param name="name">The name of the shader. This is <b>NOT</b> a full path.</param>
    /// <returns>The compiled and created SDL_GPUShader.</returns>
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
}