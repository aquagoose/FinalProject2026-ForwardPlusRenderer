using System.Numerics;

namespace Renderer;

/// <summary>
/// A light that can be added to the scene.
/// </summary>
public struct Light
{
    /// <summary>
    /// The light's type.
    /// </summary>
    public LightType Type;
    
    /// <summary>
    /// The world position. 
    /// </summary>
    public Vector3 Position;
    
    /// <summary>
    /// The light color.
    /// </summary>
    /// <remarks>The alpha value will be ignored.</remarks>
    public Color Color;

    /// <summary>
    /// The light's intensity/power.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// The radius in meters.
    /// </summary>
    public float Radius;

    public Light(LightType type, Vector3 position, Color color, float intensity, float radius)
    {
        Type = type;
        Position = position;
        Color = color;
        Intensity = intensity;
        Radius = radius;
    }

    /// <summary>
    /// Create a point light. A point light is a light which is lit equally in a sphere.
    /// </summary>
    /// <param name="position">The world position of the light.</param>
    /// <param name="color">The light color.</param>
    /// <param name="power">The light's power in lumen.</param>
    /// <param name="radius">The light's radius in meters.</param>
    /// <returns></returns>
    public static Light Point(Vector3 position, Color color, float power, float radius)
        => new Light(LightType.Point, position, color, power, radius);
}