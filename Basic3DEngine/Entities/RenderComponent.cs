using System.Numerics;
using Veldrid;

namespace Basic3DEngine.Entities;

public abstract class RenderComponent : Component
{
    protected CommandList _commandList;
    protected ResourceFactory _factory;

    protected GraphicsDevice _graphicsDevice;

    public RenderComponent(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList,
        RgbaFloat color)
    {
        _graphicsDevice = graphicsDevice;
        _factory = factory;
        _commandList = commandList;
        Color = color;
    }

    public RgbaFloat Color { get; set; }

    public abstract void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);
}