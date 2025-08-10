using System.Numerics;
using System.IO;
using Veldrid;
using Basic3DEngine.Services;
using Basic3DEngine.Entities;

namespace Basic3DEngine.Entities.Primitives;

/// <summary>
/// Geometria de cilindro simples (triangulado) com cor fixa, compatível com o pipeline básico do cubo
/// </summary>
public sealed class Cylinder : Geometry
{
    private readonly RgbaFloat _color;
    private readonly OutputDescription? _targetOutputDescription;
    private int _indexCount;

    public Cylinder(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, Vector3 position,
        RgbaFloat color, OutputDescription? targetOutputDescription = null)
        : base(graphicsDevice, factory, commandList, position)
    {
        _color = color;
        _targetOutputDescription = targetOutputDescription;
        CreateResources();
    }

    private void CreateResources()
    {
        // Geração de um cilindro de baixa resolução (laterais apenas), 24 segmentos
        const int segments = 24;
        var vertices = new VertexPositionColor[segments * 6];
        var indices = new ushort[segments * 6];

        int v = 0;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)(i * 2 * MathF.PI / segments);
            float a1 = (float)((i + 1) * 2 * MathF.PI / segments);
            var p0 = new Vector3(MathF.Cos(a0) * 0.5f, 0.5f, MathF.Sin(a0) * 0.5f);
            var p1 = new Vector3(MathF.Cos(a1) * 0.5f, 0.5f, MathF.Sin(a1) * 0.5f);
            var p2 = new Vector3(p0.X, -0.5f, p0.Z);
            var p3 = new Vector3(p1.X, -0.5f, p1.Z);
            vertices[v++] = new(p0, _color);
            vertices[v++] = new(p2, _color);
            vertices[v++] = new(p1, _color);
            vertices[v++] = new(p1, _color);
            vertices[v++] = new(p2, _color);
            vertices[v++] = new(p3, _color);
        }

        for (int i = 0; i < indices.Length; i++) indices[i] = (ushort)i;
        _indexCount = indices.Length;

        // Buffers
        _vertexBuffer = _factory.CreateBuffer(new BufferDescription(
            (uint)(vertices.Length * VertexPositionColor.SizeInBytes), BufferUsage.VertexBuffer));
        _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);

        _indexBuffer = _factory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
        _graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);

        // Reusar os mesmos shaders/layout/pipeline do cubo básico
        var shadersBasePath = ShaderLoader.GetShadersBasePath();
        var cubeShaderPath = Path.Combine(shadersBasePath, "Cube");
        var vertexShader = ShaderLoader.LoadShader(_factory, Path.Combine(cubeShaderPath, "cube.vert"), ShaderStages.Vertex);
        var fragmentShader = ShaderLoader.LoadShader(_factory, Path.Combine(cubeShaderPath, "cube.frag"), ShaderStages.Fragment);
        _shaders = new[] { vertexShader, fragmentShader };

        _uniformBuffer = _factory.CreateBuffer(new BufferDescription(192, BufferUsage.UniformBuffer));
        _resourceLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionViewWorld", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
        _resourceSet = _factory.CreateResourceSet(new ResourceSetDescription(_resourceLayout, _uniformBuffer));

        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        var pipelineDescription = new GraphicsPipelineDescription(
            BlendStateDescription.SingleOverrideBlend,
            DepthStencilStateDescription.DepthOnlyLessEqual,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(new[] { vertexLayout }, _shaders),
            new[] { _resourceLayout },
            _targetOutputDescription ?? _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription);
        _pipeline = _factory.CreateGraphicsPipeline(pipelineDescription);
    }

    public override void Update(float deltaSeconds)
    {
        var rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
        var scaleMatrix = Matrix4x4.CreateScale(Scale);
        var translationMatrix = Matrix4x4.CreateTranslation(Position);
        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (_vertexBuffer == null || _indexBuffer == null || _shaders == null || _pipeline == null || _resourceSet == null || _uniformBuffer == null)
            return;

        var ubo = new Uniforms { Projection = projectionMatrix, View = viewMatrix, World = _worldMatrix };
        _graphicsDevice.UpdateBuffer(_uniformBuffer, 0, ubo);

        commandList.SetPipeline(_pipeline);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.SetGraphicsResourceSet(0, _resourceSet);
        // quantidade de índices calculada em tempo de criação
        commandList.DrawIndexed((uint)_indexCount, 1, 0, 0, 0);
    }

    public override void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _resourceSet?.Dispose();
        _resourceLayout?.Dispose();
        if (_shaders != null)
            foreach (var s in _shaders) s?.Dispose();
        _pipeline?.Dispose();
    }

    private struct Uniforms
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 World;
    }
}


