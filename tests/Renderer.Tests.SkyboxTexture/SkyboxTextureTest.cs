using System.Drawing;
using System.Numerics;
using Renderer.Materials;
using Renderer.Primitives;
using Renderer.Tests.Common;
using Rectangle = Renderer.Math.Rectangle;

namespace Renderer.Tests.SkyboxTexture;

public class SkyboxTextureTest() : TestBase("Skybox Texture Test")
{
    private Skybox _skybox = null!;
    private Texture _texture = null!;
    private Material _material = null!;
    private Renderable _renderable = null!;

    private Vector3[] _cubePos;
    
    private float _rotation;
    
    protected override void Load()
    {
        _skybox = new Skybox(Renderer, "Content/Skybox/right.jpg", "Content/Skybox/left.jpg", "Content/Skybox/top.jpg",
            "Content/Skybox/bottom.jpg", "Content/Skybox/front.jpg", "Content/Skybox/back.jpg");

        _texture = new Texture(Renderer, "Content/DEBUG.png");
        _material = new UnlitMaterial(Renderer, _texture);

        Cube cube = new Cube();
        _renderable = new Renderable(Renderer, _material, cube.Vertices, cube.Indices);

        _cubePos = new Vector3[60];
        Random random = new Random();
        const int range = 10;
        for (int i = 0; i < _cubePos.Length; i++)
        {
            int x;
            int y;
            int z;

            do
            {
                x = random.Next(-range, range);
                y = random.Next(-range, range);
                z = random.Next(-range, range);
            } while (x == 0 && y == 0 && z == 0);
            
            Vector3 pos = new Vector3(x, y, z);
            if (_cubePos.Contains(pos))
            {
                i--;
                continue;
            }

            _cubePos[i] = pos;
        }
    }

    protected override void Loop(float dt)
    {
        foreach (Vector3 cube in _cubePos)
        {
            Renderer.Draw(_renderable, Matrix4x4.CreateTranslation(cube));
        }
        
        _rotation += dt * 0.2f;
        if (_rotation >= float.Pi * 2)
            _rotation -= float.Pi * 2;

        float sinRot = float.Sin(_rotation) * 0.6f;
        
        Camera camera = Camera.Perspective(new Vector3(0, 0, 0), Quaternion.CreateFromYawPitchRoll(_rotation, sinRot, 0), float.DegreesToRadians(75),
            new Rectangle(0, 0, Size.Width / 2, Size.Height), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in camera);
        
        Camera camera2 = Camera.Perspective(new Vector3(0, 0, 0), Quaternion.CreateFromYawPitchRoll(-_rotation, -sinRot, 0), float.DegreesToRadians(75),
            new Rectangle((int) Size.Width / 2, 0, Size.Width / 2, Size.Height / 2), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in camera2);
        
        Camera camera3 = Camera.Perspective(new Vector3(10, -4, 2), Quaternion.CreateFromYawPitchRoll(_rotation, sinRot, 0), float.DegreesToRadians(75),
            new Rectangle((int) Size.Width / 2, (int) Size.Height / 2, Size.Width / 2, Size.Height / 2), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in camera3);
    }

    public override void Dispose()
    {
        _renderable.Dispose();
        _material.Dispose();
        _texture.Dispose();
        _skybox.Dispose();
        base.Dispose();
    }
}