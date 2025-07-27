using BepuPhysics;
using BepuPhysics.Collidables;
using Basic3DEngine.Core.Interfaces;
using System;

namespace Basic3DEngine.Physics.Shapes;

/// <summary>
/// Representa uma esfera física
/// </summary>
public sealed class SphereShape(float radius) : IPhysicsShape
{
    /// <summary>
    /// Raio da esfera
    /// </summary>
    public float Radius { get; } = Math.Max(0.001f, radius); // Garantir raio mínimo

    /// <inheritdoc/>
    public TypedIndex CreateShape(BepuPhysics.Collidables.Shapes shapes)
    {
        var sphere = new Sphere(Radius);
        return shapes.Add(sphere);
    }

    /// <inheritdoc/>
    public BodyInertia ComputeInertia(float mass)
    {
        // Garantir massa mínima para evitar divisão por zero
        var safeMass = Math.Max(0.001f, mass);
        var sphere = new Sphere(Radius);
        return sphere.ComputeInertia(safeMass);
    }
} 