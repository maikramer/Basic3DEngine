using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Basic3DEngine.Entities.Primitives;

namespace Basic3DEngine.Entities;

/// <summary>
/// Renderer que compõe múltiplas geometrias simples com poses locais relativas a um GameObject.
/// </summary>
public sealed class CompoundRenderComponent : RenderComponent
{
    private readonly List<Entry> _entries = new();
    private readonly OutputDescription? _targetOutputDescription;

    private readonly struct Entry
    {
        public readonly Geometry Geometry;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 LocalRotation;
        public readonly Vector3 LocalScale;
        public Entry(Geometry geometry, Vector3 localPosition, Vector3 localRotation, Vector3 localScale)
        {
            Geometry = geometry;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }
    }

    public CompoundRenderComponent(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList,
        OutputDescription? targetOutputDescription = null)
        : base(graphicsDevice, factory, commandList, RgbaFloat.White)
    {
        _targetOutputDescription = targetOutputDescription;
    }

    public void AddCube(RgbaFloat color, Vector3 localPosition, Vector3 localRotation, Vector3 localScale)
    {
        var geo = new Cube(_graphicsDevice, _factory, _commandList, Vector3.Zero, color, _targetOutputDescription);
        _entries.Add(new Entry(geo, localPosition, localRotation, localScale));
    }

    public void AddCylinder(RgbaFloat color, Vector3 localPosition, Vector3 localRotation, Vector3 localScale)
    {
        var geo = new Cylinder(_graphicsDevice, _factory, _commandList, Vector3.Zero, color, _targetOutputDescription);
        _entries.Add(new Entry(geo, localPosition, localRotation, localScale));
    }

    public void AddSphere(RgbaFloat color, Vector3 localPosition, Vector3 localRotation, float radiusScale)
    {
        var geo = new Icosphere(_graphicsDevice, _factory, _commandList, Vector3.Zero, color, 2, _targetOutputDescription);
        _entries.Add(new Entry(geo, localPosition, localRotation, new Vector3(radiusScale)));
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (GameObject == null) return;
        foreach (var e in _entries)
        {
            var worldPos = GameObject.Position + Vector3.Transform(e.LocalPosition, Matrix4x4.CreateFromYawPitchRoll(GameObject.Rotation.Y, GameObject.Rotation.X, GameObject.Rotation.Z));
            e.Geometry.Position = worldPos;
            e.Geometry.Rotation = GameObject.Rotation + e.LocalRotation;
            e.Geometry.Scale = e.LocalScale;
            e.Geometry.Render(commandList, viewMatrix, projectionMatrix);
        }
    }

    public override void Update(float deltaTime)
    {
        // Atualizar geometrias se necessário
        foreach (var e in _entries)
        {
            e.Geometry.Update(deltaTime);
        }
    }

    public void Dispose()
    {
        foreach (var e in _entries)
        {
            e.Geometry.Dispose();
        }
    }
}


