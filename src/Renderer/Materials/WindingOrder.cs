namespace Renderer.Materials;

/// <summary>
/// Represents the ways in which vertices can be "wound".
/// </summary>
public enum WindingOrder
{
    /// <summary>
    /// The vertices are wound counter/anti-clockwise
    /// </summary>
    CounterClockwise,
    
    /// <summary>
    /// The vertices are wound clockwise.
    /// </summary>
    Clockwise
}