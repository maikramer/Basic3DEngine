using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Basic3DEngine.Core.Interfaces;
using System;

namespace Basic3DEngine.Physics.Shapes;

/// <summary>
/// Representa uma caixa física
/// </summary>
public sealed class BoxShape(Vector3 size) : IPhysicsShape
{
    /// <summary>
    /// Tamanho da caixa (largura, altura, profundidade)
    /// </summary>
    public Vector3 Size { get; } = new Vector3(
        Math.Max(0.001f, size.X), 
        Math.Max(0.001f, size.Y), 
        Math.Max(0.001f, size.Z)
    ); // Garantir dimensões mínimas

    /// <summary>
    /// Metade do tamanho da caixa
    /// </summary>
    public Vector3 HalfSize => Size * 0.5f;

    /// <inheritdoc/>
    public TypedIndex CreateShape(BepuPhysics.Collidables.Shapes shapes)
    {
        var box = new Box(Size.X, Size.Y, Size.Z);
        return shapes.Add(box);
    }

    /// <inheritdoc/>
    public BodyInertia ComputeInertia(float mass)
    {
        // Garantir massa mínima para evitar divisão por zero
        var safeMass = Math.Max(0.001f, mass);
        var box = new Box(Size.X, Size.Y, Size.Z);
        return box.ComputeInertia(safeMass);
    }
} 