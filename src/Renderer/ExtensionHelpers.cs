using System.Runtime.InteropServices;
using SDL3;

namespace Renderer;

/// <summary>
/// Contains a series of extensions to make the API easier to use.
/// </summary>
public static class ExtensionHelpers
{
    extension(PixelFormat format)
    {
        /// <summary>
        /// Gets the number of bytes per pixel.
        /// </summary>
        public uint BytesPerPixel
        {
            get
            {
                return format switch
                {
                    PixelFormat.RGBA8 => 4,
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
            }
        }
    }

    extension(ShaderCross)
    {
        // SDL3-CS's ShaderCross bindings have the incorrect signature for this function, so I'm redefining it here with the
        // correct signature.
        [DllImport("SDL3_shadercross", EntryPoint = "SDL_ShaderCross_CompileComputePipelineFromSPIRV")]
        public static extern IntPtr CompileComputePipelineFromSPIRV(IntPtr device, in ShaderCross.SPIRVInfo spirvInfo,
            in ShaderCross.ComputePipelineMetadata metadata, uint props);
    }
}