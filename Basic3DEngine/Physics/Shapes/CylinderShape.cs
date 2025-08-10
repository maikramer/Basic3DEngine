using BepuPhysics;
using BepuPhysics.Collidables;
using Basic3DEngine.Core.Interfaces;
using System;

namespace Basic3DEngine.Physics.Shapes;

/// <summary>
/// Representa um cilindro físico (eixo ao longo de Y local)
/// </summary>
public sealed class CylinderShape(float radius, float length) : IPhysicsShape
{
    /// <summary>
    /// Raio do cilindro
    /// </summary>
    public float Radius { get; } = Math.Max(0.001f, radius);

    /// <summary>
    /// Comprimento total do cilindro ao longo do eixo Y local
    /// </summary>
    public float Length { get; } = Math.Max(0.001f, length);

    /// <inheritdoc/>
    public TypedIndex CreateShape(BepuPhysics.Collidables.Shapes shapes)
    {
        var cyl = new Cylinder(Radius, Length);
        return shapes.Add(cyl);
    }

    /// <inheritdoc/>
    public BodyInertia ComputeInertia(float mass)
    {
        var safeMass = Math.Max(0.001f, mass);
        var cyl = new Cylinder(Radius, Length);
        return cyl.ComputeInertia(safeMass);
    }

    public float ApproximateRadius => MathF.Sqrt(Radius * Radius + (Length * 0.5f) * (Length * 0.5f));
}


