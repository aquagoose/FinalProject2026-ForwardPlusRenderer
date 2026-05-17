namespace Renderer.Materials;

/// <summary>
/// Defines various material parameters that cannot be changed once a material is created.
/// </summary>
public struct MaterialInfo
{
    /// <summary>
    /// Determines which faces (if any) to cull.
    /// </summary>
    public CullFace CullFace;

    /// <summary>
    /// The winding order of the vertices.
    /// </summary>
    public WindingOrder WindingOrder;
    
    /// <summary>
    /// Enable transparency effects.
    /// </summary>
    public bool EnableTransparency;
}