namespace Renderer.Materials;

/// <summary>
/// Represents the ways a face can be culled.
/// </summary>
public enum CullFace
{
    /// <summary>
    /// The back faces will be culled.
    /// </summary>
    Back,
    
    /// <summary>
    /// The front faces will be culled.
    /// </summary>
    Front,
    
    /// <summary>
    /// No faces will be culled.
    /// </summary>
    None
}