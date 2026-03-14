using System.Drawing;
using System.Numerics;

namespace Renderer;

public struct Camera
{
    public Matrix4x4 Projection;

    public Matrix4x4 View;

    // TODO: Custom rectangle struct
    public Rectangle Viewport;

    public Camera(Matrix4x4 projection, Matrix4x4 view, Rectangle viewport)
    {
        Projection = projection;
        View = view;
        Viewport = viewport;
    }

    public static Camera Perspective(Vector3 position, Quaternion orientation, float fov, Rectangle viewport,
        float near, float far)
    {
        float aspect = viewport.Width / (float) viewport.Height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);

        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, orientation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, orientation);
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, position + forward, up);

        return new Camera(projection, view, viewport);
    }
}