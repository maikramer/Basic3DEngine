namespace Basic3DEngine.Entities;

public abstract class Component
{
    public GameObject GameObject { get; internal set; }
    public bool Enabled { get; set; } = true;

    public virtual void Update(float deltaTime)
    {
        // Implementação padrão vazia
    }
}