using System.Drawing;
using System.Numerics;
using Rectangle = Renderer.Math.Rectangle;

namespace Renderer;

/// <summary>
/// Represents a viewport that the scene can be rendered with.
/// </summary>
public struct Camera
{
    /// <summary>
    /// The projection matrix.
    /// </summary>
    public Matrix4x4 Projection;

    /// <summary>
    /// The view matrix.
    /// </summary>
    public Matrix4x4 View;

    // TODO: Custom rectangle struct
    /// <summary>
    /// The viewport region on screen.
    /// </summary>
    public Rectangle Viewport;

    /// <summary>
    /// The skybox for this camera, if any.
    /// </summary>
    public Skybox? Skybox;

    /// <summary>
    /// Create a <see cref="Camera"/> with a projection &amp; view matrix.
    /// </summary>
    /// <param name="projection">The projection matrix.</param>
    /// <param name="view">The view matrix.</param>
    /// <param name="viewport">The viewport region on screen.</param>
    /// <param name="skybox">The skybox for this camera, if any.</param>
    public Camera(Matrix4x4 projection, Matrix4x4 view, Rectangle viewport, Skybox? skybox = null)
    {
        Projection = projection;
        View = view;
        Viewport = viewport;
        Skybox = skybox;
    }

    /// <summary>
    /// Create a perspective <see cref="Camera"/>.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="orientation">The orientation/rotation.</param>
    /// <param name="fov">The field of view, in radians.</param>
    /// <param name="viewport">The viewport region on screen.</param>
    /// <param name="near">The near plane distance.</param>
    /// <param name="far">The far plane distance.</param>
    /// <param name="skybox">The skybox for this camera, if any.</param>
    /// <returns>A <see cref="Camera"/> with the matrices set up for perspective.</returns>
    public static Camera Perspective(Vector3 position, Quaternion orientation, float fov, Rectangle viewport,
        float near, float far, Skybox? skybox = null)
    {
        float aspect = viewport.Width / (float) viewport.Height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);

        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, orientation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, orientation);
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, position + forward, up);

        return new Camera(projection, view, viewport, skybox);
    }
}