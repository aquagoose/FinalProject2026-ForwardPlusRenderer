using SDL3;

namespace Renderer.Utils;

internal static class ShaderUtils
{
    public static unsafe IntPtr LoadShader(IntPtr device, ShaderCross.ShaderStage stage, string name)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Shaders", name);
        string? includeDir = Path.GetDirectoryName(fullPath);
        SDL.GPUShaderFormat format = SDL.GetGPUShaderFormats(device);
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
            Source = hlsl,
            Entrypoint = entryPoint,
            IncludeDir = includeDir
        };
        ShaderCross.CompileSPIRVFromHLSL()
        
        fixed (byte* pSpirv = spirv)
        {
            var metadata =
                (ShaderCross.GraphicsShaderMetadata*) ShaderCross.ReflectGraphicsSPIRV((IntPtr) pSpirv,
                    (nuint) spirv.Length, 0);

            ref var resourceInfo = ref metadata->ResourceInfo;
            
            ShaderCross.SPIRVInfo spirvInfo = new()
            {
                Entrypoint = 
            }
        }
    }
}