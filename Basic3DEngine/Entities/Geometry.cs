using System.Numerics;
using Veldrid;

namespace Basic3DEngine.Entities;

public abstract class Geometry
{
    protected CommandList _commandList;
    protected ResourceFactory _factory;
    protected GraphicsDevice _graphicsDevice;
    protected DeviceBuffer? _indexBuffer;
    protected Pipeline? _pipeline;
    protected ResourceLayout? _resourceLayout;
    protected ResourceSet? _resourceSet;
    protected Shader[]? _shaders;
    protected DeviceBuffer? _uniformBuffer;

    protected DeviceBuffer? _vertexBuffer;

    protected Matrix4x4 _worldMatrix = Matrix4x4.Identity;

    public Geometry(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, Vector3 position)
    {
        _graphicsDevice = graphicsDevice;
        _factory = factory;
        _commandList = commandList;
        Position = position;
    }

    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;

    public abstract void Update(float deltaSeconds);
    public abstract void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);
    public abstract void Dispose();
}

// Estrutura para o buffer de uniformes
internal struct UniformBufferObject
{
    public Matrix4x4 Projection;
    public Matrix4x4 View;
    public Matrix4x4 World;
    public Matrix4x4 ShadowMatrix;
}