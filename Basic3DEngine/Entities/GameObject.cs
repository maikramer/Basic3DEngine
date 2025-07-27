using System.Numerics;

namespace Basic3DEngine.Entities;

public class GameObject
{
    private readonly Dictionary<Type, Component> _components;

    public GameObject(string name = "GameObject")
    {
        Name = name;
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
        _components = new Dictionary<Type, Component>();
    }

    public string Name { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; }

    public void AddComponent<T>(T component) where T : Component
    {
        component.GameObject = this;
        _components[typeof(T)] = component;
    }

    public T GetComponent<T>() where T : Component
    {
        // Primeiro tenta buscar pelo tipo exato
        if (_components.TryGetValue(typeof(T), out var component)) 
            return (T)component;
            
        // Se não encontrar, busca por tipos que herdam de T
        foreach (var kvp in _components)
        {
            if (typeof(T).IsAssignableFrom(kvp.Key))
                return (T)kvp.Value;
        }
        
        return null;
    }

    public bool HasComponent<T>() where T : Component
    {
        return _components.ContainsKey(typeof(T));
    }

    public void RemoveComponent<T>() where T : Component
    {
        if (_components.ContainsKey(typeof(T))) _components.Remove(typeof(T));
    }

    public IEnumerable<Component> GetAllComponents()
    {
        return _components.Values;
    }

    public virtual void Update(float deltaTime)
    {
        foreach (var component in _components.Values)
            if (component.Enabled)
                component.Update(deltaTime);
    }
}