using System;
using System.Collections.Generic;
using System.Numerics;
using Basic3DEngine.Core.Interfaces;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuShapes = BepuPhysics.Collidables.Shapes;
using BepuUtilities.Memory;

namespace Basic3DEngine.Physics.Shapes;

/// <summary>
/// Forma composta por várias primitivas convexas em poses locais.
/// </summary>
public sealed class CompoundShape : IRequiresBufferPoolShape
{
    public readonly struct Child
    {
        public readonly IPhysicsShape Shape;
        public readonly RigidPose LocalPose;
        public readonly float Weight;
        public Child(IPhysicsShape shape, in RigidPose localPose, float weight)
        {
            Shape = shape;
            LocalPose = localPose;
            Weight = MathF.Max(0.0001f, weight);
        }
    }

    private readonly List<Child> _children;

    public CompoundShape(IEnumerable<Child> children)
    {
        _children = new List<Child>(children);
        if (_children.Count == 0) throw new ArgumentException("CompoundShape precisa de ao menos um filho");
    }

    public CompoundShape AddChild(IPhysicsShape shape, in RigidPose localPose, float weight = 1f)
    {
        _children.Add(new Child(shape, localPose, weight));
        return this;
    }

    public TypedIndex CreateShape(BepuShapes shapes)
    {
        throw new InvalidOperationException("CompoundShape requer BufferPool; use CreateShape(Shapes, BufferPool)");
    }

    public TypedIndex CreateShape(BepuShapes shapes, BufferPool pool)
    {
        using var builder = new CompoundBuilder(pool, shapes, _children.Count);
        foreach (var c in _children)
        {
            // Se o filho também requer pool, garanta a criação correta
            TypedIndex childIndex = c.Shape is IRequiresBufferPoolShape req
                ? req.CreateShape(shapes, pool)
                : c.Shape.CreateShape(shapes);
            // Obter inércia local para cálculo correto de massa
            var childInertia = c.Shape.ComputeInertia(c.Weight);
            builder.Add(childIndex, c.LocalPose, childInertia);
        }

        builder.BuildDynamicCompound(out var childrenBuffer, out var compoundInertia, out _);
        var compound = new Compound(childrenBuffer);
        return shapes.Add(compound);
    }

    public BodyInertia ComputeInertia(float mass)
    {
        // Dividir massa total igualmente por simplicidade
        int n = _children.Count;
        var masses = new float[n];
        var poses = new RigidPose[n];
        var inertias = new BepuUtilities.Symmetric3x3[n];
        for (int i = 0; i < n; i++)
        {
            var m = mass * (_children[i].Weight / TotalWeight());
            var childInertia = _children[i].Shape.ComputeInertia(m);
            inertias[i] = childInertia.InverseInertiaTensor; // usado pelos utilitários de composto
            poses[i] = _children[i].LocalPose;
            masses[i] = m;
        }
        // Usar utilitários para compor inércia aproximada
        return CompoundBuilder.ComputeInverseInertia(poses, inertias, masses);
    }

    private float TotalWeight()
    {
        float sum = 0f;
        foreach (var c in _children) sum += c.Weight;
        return MathF.Max(0.0001f, sum);
    }

    public float ApproximateRadius => 1f; // não usado diretamente para CCD aqui
}


