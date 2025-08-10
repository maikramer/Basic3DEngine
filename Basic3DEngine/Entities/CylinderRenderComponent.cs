using System.Numerics;
using Basic3DEngine.Entities.Primitives;
using Veldrid;

namespace Basic3DEngine.Entities;

public sealed class CylinderRenderComponent : RenderComponent
{
    private readonly Cylinder _cylinder;
    private readonly OutputDescription? _targetOutputDescription;

    public CylinderRenderComponent(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList,
        RgbaFloat color, OutputDescription? targetOutputDescription = null)
        : base(graphicsDevice, factory, commandList, color)
    {
        _targetOutputDescription = targetOutputDescription;
        _cylinder = new Cylinder(graphicsDevice, factory, commandList, Vector3.Zero, color, _targetOutputDescription);
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (GameObject == null) return;
        _cylinder.Position = GameObject.Position;
        _cylinder.Rotation = GameObject.Rotation;
        _cylinder.Scale = GameObject.Scale;
        _cylinder.Render(commandList, viewMatrix, projectionMatrix);
    }
}


