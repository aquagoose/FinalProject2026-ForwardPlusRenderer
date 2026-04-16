using System.Runtime.CompilerServices;
using Renderer.Tests.Common;
using Renderer.Utils;
using SDL3;

namespace Renderer.Tests.ComputeShader;

// This test is a lot more manual
// I've had to make some internal classes public to do this
// This test may be removed at a later date once I've figured out compute shaders but probably not!
public class ComputeShaderTest() : TestBase("Compute Shader Test")
{
    private IntPtr _texture;
    private IntPtr _pipeline;
    
    protected override void Load()
    {
        IntPtr device = Renderer.Device;
        
        // Create the storage texture that the compute shader will write to.
        _texture = SDLUtils.CreateTexture(device, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureFormat.R8G8B8A8Unorm,
            800, 600, 1, 1, SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ComputeStorageWrite);

        const string entryPoint = "CSMain";
        IntPtr spirv = ShaderUtils.CompileShader(ShaderCross.ShaderStage.Compute, "SimpleComputeShader.hlsl",
            entryPoint, out nuint size);

        ShaderCross.SPIRVInfo spirvInfo = new()
        {
            ByteCode = spirv,
            ByteCodeSize = size,
            ManagedEntrypoint = entryPoint,
            ShaderStage = ShaderCross.ShaderStage.Compute
        };

        ShaderCross.ComputePipelineMetadata metadata = new()
        {
            ThreadCountX = 1,
            ThreadCountY = 1,
            ThreadCountZ = 1,
            NumReadWriteStorageTextures = 1
        };

        _pipeline = ShaderUtils.CompileComputePipelineFromSPIRV(device, in spirvInfo, in metadata, 0)
            .Check("Compile compute pipeline");
    }
}