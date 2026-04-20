namespace Demo.Demos;

public abstract class Demo(string name) : IDisposable
{
    protected Renderer.Renderer Renderer => DemoApp.Renderer;
    
    public virtual void Initialize() { }

    public virtual void Update(float dt) { }

    public virtual void Draw() { }

    public virtual void Dispose() { }
}