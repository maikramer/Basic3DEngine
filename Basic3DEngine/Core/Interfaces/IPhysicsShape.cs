using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Basic3DEngine.Core.Interfaces;

/// <summary>
/// Interface para formas físicas na engine
/// </summary>
public interface IPhysicsShape
{
    /// <summary>
    /// Cria o shape no BepuPhysics
    /// </summary>
    TypedIndex CreateShape(BepuPhysics.Collidables.Shapes shapes);
    
    /// <summary>
    /// Calcula a inércia para a forma
    /// </summary>
    BodyInertia ComputeInertia(float mass);
} 