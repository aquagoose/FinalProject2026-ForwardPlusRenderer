using System.Numerics;
using Hexa.NET.ImGui;
using Renderer;
using Renderer.Materials;
using Renderer.Math;
using Renderer.Primitives;

namespace Demo.Demos;

public class TransparencyDemo() : Demo("Transparency Demo")
{
    private Skybox _skybox;
    private Texture _texture;
    
    private Material _transparentMaterial;
    private Renderable _transparentRenderable;

    private Material _screenDoorMaterial;
    private Renderable _screenDoorRenderable;

    private float _value;
    
    public override void Initialize()
    {
        _skybox = new Skybox(Renderer, "Content/Skybox/Standard/right.jpg", "Content/Skybox/Standard/left.jpg", "Content/Skybox/Standard/top.jpg",
            "Content/Skybox/Standard/bottom.jpg", "Content/Skybox/Standard/front.jpg", "Content/Skybox/Standard/back.jpg");
        _texture = new Texture(Renderer, "Content/DEBUG.png");
        
        _transparentMaterial = new UnlitMaterial(Renderer, _texture, new MaterialInfo { EnableTransparency = true });
        _screenDoorMaterial = new UnlitMaterial(Renderer, _texture, useScreenDoor: true);

        Cube cube = new Cube();
        
        Vertex[] vertices = cube.Vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i].Color.A = 0.5f;
        
        _transparentRenderable = new Renderable(Renderer, _transparentMaterial, vertices, cube.Indices);
        _screenDoorRenderable = new Renderable(Renderer, _screenDoorMaterial, vertices, cube.Indices);
    }

    public override void DisplayUI()
    {
        base.DisplayUI();

        Size rendererSize = Renderer.Size;
        const uint size = 48;
        
        {
            const string text = "Deferred";
            Size textSize = ImGui.MeasureText(size, text);
            ImGui.DrawText(new Vector2(0, rendererSize.Height - textSize.Height), size, text, Color.White);
        }

        {
            const string text = "Forward+";
            Size textSize = ImGui.MeasureText(size, text);
            ImGui.DrawText(new Vector2(rendererSize.Width / 2, rendererSize.Height - textSize.Height), size, text, Color.White);
        }
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        _value += dt;
        if (_value >= float.Pi * 2)
            _value -= float.Pi * 2;
    }

    public override void Draw()
    {
        Renderer.BackgroundColor = Color.CornflowerBlue;

        for (int i = 0; i < 10; i++)
        {
            float value = _value + (i * 0.3f);
            Renderer.Draw(_transparentRenderable,
                Matrix4x4.CreateFromYawPitchRoll(value, value, value) * Matrix4x4.CreateTranslation(0, (i * 0.5f) - 0.3f,  -(i * 2) - 3));

            Renderer.Draw(_screenDoorRenderable,
                Matrix4x4.CreateFromYawPitchRoll(value, value, value) * Matrix4x4.CreateTranslation(0, (i * 0.5f) - 0.3f, (i * 2) + 3));
        }

        Size halfWidthSize = Renderer.Size with { Width = Renderer.Size.Width / 2 };
        int halfWidth = (int) Renderer.Size.Width / 2;
        Offset halfWidthOffset = new Offset(halfWidth, 0);

        Camera screenDoorCamera = Camera.Perspective(new Vector3(0, 0, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.Pi), float.DegreesToRadians(45),
            new Rectangle(Offset.Zero, halfWidthSize), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in screenDoorCamera);

        Camera transparentCamera = Camera.Perspective(new Vector3(0, 0, 0), Quaternion.Identity, float.DegreesToRadians(45),
            new Rectangle(halfWidthOffset, halfWidthSize), 0.1f, 100f, _skybox);
        Renderer.AddCamera(in transparentCamera);
        
        base.Draw();
    }

    public override void Dispose()
    {
        _screenDoorRenderable.Dispose();
        _transparentRenderable.Dispose();
        
        _screenDoorMaterial.Dispose();
        _transparentMaterial.Dispose();
        
        _texture.Dispose();
        _skybox.Dispose();
    }
}