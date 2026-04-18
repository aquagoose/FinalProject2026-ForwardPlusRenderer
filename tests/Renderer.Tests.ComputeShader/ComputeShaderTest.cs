using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Materials;
using Renderer.Tests.Common;
using Renderer.Utils;
using SDL3;
using Plane = Renderer.Primitives.Plane;

namespace Renderer.Tests.ComputeShader;

// This test is a lot more manual
// I've had to make some internal classes public to do this
// This test may be removed at a later date once I've figured out compute shaders but probably not!
public class ComputeShaderTest() : TestBase("Compute Shader Test")
{
    private IntPtr _texture;
    private IntPtr _pipeline;
    private Material _material;
    private Renderable _renderable;
    
    protected override void Load()
    {
        IntPtr device = Renderer.Device;
        
        // Create the storage texture that the compute shader will write to.
        _texture = SDLUtils.CreateTexture(device, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureFormat.R32G32B32A32Float,
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
            ThreadCountX = 8,
            ThreadCountY = 8,
            ThreadCountZ = 1,
            NumReadWriteStorageTextures = 1
        };

        _pipeline = ShaderCross.CompileComputePipelineFromSPIRV(device, in spirvInfo, in metadata, 0)
            .Check("Compile compute pipeline");

        _material = new UnlitMaterial(Renderer, new Texture(Renderer, _texture), new MaterialInfo()
        {
            CullFace = CullFace.None
        });
        
        Plane plane = new Plane();
        _renderable = new Renderable(Renderer, _material, plane.Vertices, plane.Indices);
    }

    protected override void Loop(float dt)
    {
        IntPtr cb = SDL.AcquireGPUCommandBuffer(Renderer.Device).Check("Acquire command buffer");

        // Yikes... What horrible signature for a function. Inefficient too. I'll have to PR this back to SDL3-CS as well.
        IntPtr pass = SDL.BeginGPUComputePass(cb, [
            new SDL.GPUStorageTextureReadWriteBinding()
            {
                Texture = _texture,
                Layer = 0,
                MipLevel = 0,
                Cycle = 1
            }
        ], 1, [], 0).Check("Begin compute pass");
        
        SDL.BindGPUComputePipeline(pass, _pipeline);
        SDL.DispatchGPUCompute(pass, 800 / 8, 600 / 8, 1);
        
        SDL.EndGPUComputePass(pass);
        SDL.SubmitGPUCommandBuffer(cb).Check("Submit command buffer");
        
        Renderer.BackgroundColor = Color.CornflowerBlue;
        Renderer.AddCamera(Camera.Perspective(new Vector3(0, 0, 3), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(0, 0, 1280, 720), 0.1f, 100f));
        
        Renderer.Draw(_renderable, Matrix4x4.Identity);
    }
}