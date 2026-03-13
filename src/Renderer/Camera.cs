using System.Numerics;

namespace Renderer;

public struct Camera
{
    public Matrix4x4 Projection;

    public Matrix4x4 View;

    public Vector4 Position;

    public Camera(Matrix4x4 projection, Matrix4x4 view, Vector4 position)
    {
        Projection = projection;
        View = view;
        Position = position;
    }

    public static Camera Perspective(Vector3 position, Quaternion orientation, float fov, float aspect, float near, float far)
    {
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);

        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, orientation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, orientation);
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, position + forward, up);

        return new Camera(projection, view, new Vector4(position, 0));
    }
}