using System.Numerics;
using System.Text;
using Veldrid;

namespace Basic3DEngine.Entities;

public class Cube : Geometry
{
    private static StreamWriter _logFile;
    private readonly RgbaFloat _color;

    public Cube(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, Vector3 position,
        RgbaFloat color)
        : base(graphicsDevice, factory, commandList, position)
    {
        _color = color;
        CreateResources();
    }

    private void CreateResources()
    {
        Log("Cube.CreateResources() called");
        // Definição dos vértices do cubo com a cor especificada
        // Os vértices são definidos em torno da origem e serão transformados pela matriz world
        VertexPositionColor[] vertices =
        {
            // Frente
            new(new Vector3(-0.5f, -0.5f, -0.5f), _color),
            new(new Vector3(0.5f, -0.5f, -0.5f), _color),
            new(new Vector3(0.5f, 0.5f, -0.5f), _color),
            new(new Vector3(-0.5f, 0.5f, -0.5f), _color),
            // Trás
            new(new Vector3(-0.5f, -0.5f, 0.5f), _color),
            new(new Vector3(0.5f, -0.5f, 0.5f), _color),
            new(new Vector3(0.5f, 0.5f, 0.5f), _color),
            new(new Vector3(-0.5f, 0.5f, 0.5f), _color)
        };

        // Índices para formar os triângulos
        ushort[] indices =
        {
            // Frente
            0, 1, 2, 0, 2, 3,
            // Topo
            3, 2, 6, 3, 6, 7,
            // Trás
            7, 6, 5, 7, 5, 4,
            // Fundo
            4, 5, 1, 4, 1, 0,
            // Direita
            1, 5, 6, 1, 6, 2,
            // Esquerda
            4, 0, 3, 4, 3, 7
        };

        // Criação dos buffers
        try
        {
            Log("Creating vertex buffer");
            _vertexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(vertices.Length * VertexPositionColor.SizeInBytes),
                BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);
            Log("Vertex buffer created successfully");

            Log("Creating index buffer");
            _indexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(indices.Length * sizeof(ushort)),
                BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);
            Log("Index buffer created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating buffers: {ex.Message}");
            throw;
        }

        // Criação dos shaders
        Log("Creating shaders");
        CreateShaders();
        Log("Shaders created successfully");

        // Criação do buffer de uniformes para as matrizes
        // 3 matrizes de 4x4 floats (4 bytes cada) = 3 * 16 * 4 = 192 bytes
        try
        {
            Log("Creating uniform buffer");
            _uniformBuffer = _factory.CreateBuffer(new BufferDescription(
                192,
                BufferUsage.UniformBuffer));
            Log("Uniform buffer created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating uniform buffer: {ex.Message}");
            throw;
        }

        // Criação do layout de recursos
        try
        {
            Log("Creating resource layout");
            _resourceLayout = _factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "ProjectionViewWorld",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Vertex)));
            Log("Resource layout created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating resource layout: {ex.Message}");
            throw;
        }

        // Criação do conjunto de recursos
        try
        {
            Log("Creating resource set");
            _resourceSet = _factory.CreateResourceSet(
                new ResourceSetDescription(
                    _resourceLayout,
                    _uniformBuffer));
            Log("Resource set created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating resource set: {ex.Message}");
            throw;
        }

        // Criação do pipeline
        Log("Creating pipeline");
        CreatePipeline();
        Log("Pipeline created successfully");
        Log("Cube.CreateResources() completed");
    }

    private void CreateShaders()
    {
        Log("Cube.CreateShaders() called");
        var vertexShaderCode = @"
#version 330

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;

uniform ProjectionViewWorld
{
    mat4 projection;
    mat4 view;
    mat4 world;
};

out vec4 fsin_Color;

void main()
{
    vec4 worldPosition = world * vec4(Position, 1);
    vec4 viewPosition = view * worldPosition;
    vec4 clipPosition = projection * viewPosition;
    gl_Position = clipPosition;
    fsin_Color = Color;
}";

        var fragmentShaderCode = @"
#version 330

in vec4 fsin_Color;
out vec4 OutputColor;

void main()
{
    OutputColor = fsin_Color;
}";

        var vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(vertexShaderCode),
            "main");

        var fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(fragmentShaderCode),
            "main");

        _shaders = new Shader[2];
        try
        {
            Log("Creating vertex shader");
            _shaders[0] = _factory.CreateShader(vertexShaderDesc);
            Log("Creating fragment shader");
            _shaders[1] = _factory.CreateShader(fragmentShaderDesc);
            Log("Shaders created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating shaders: {ex.Message}");
            throw;
        }
    }

    private void CreatePipeline()
    {
        Log("Cube.CreatePipeline() called");
        // Descrição do layout dos vértices
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        // Descrição do pipeline
        var pipelineDescription = new GraphicsPipelineDescription(
            BlendStateDescription.SingleOverrideBlend,
            DepthStencilStateDescription.DepthOnlyLessEqual,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                new[] { vertexLayout },
                _shaders),
            new[] { _resourceLayout },
            _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription);

        try
        {
            Log("Creating graphics pipeline");
            _pipeline = _factory.CreateGraphicsPipeline(pipelineDescription);
            Log("Graphics pipeline created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating pipeline: {ex.Message}");
            throw;
        }
    }

    public override void Update(float deltaSeconds)
    {
        // Atualiza a matriz de transformação
        var rotationMatrix = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, Rotation.Y);
        var scaleMatrix = Matrix4x4.CreateScale(Scale);
        var translationMatrix = Matrix4x4.CreateTranslation(Position);
        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        Log($"Cube.Render() called with position: {Position}, scale: {Scale}");

        if (_vertexBuffer == null || _indexBuffer == null || _shaders == null ||
            _pipeline == null || _resourceSet == null || _uniformBuffer == null)
        {
            Log("Cube.Render() early exit - one or more resources are null");
            Log($"vertexBuffer: {_vertexBuffer != null}");
            Log($"indexBuffer: {_indexBuffer != null}");
            Log($"shaders: {_shaders != null}");
            Log($"pipeline: {_pipeline != null}");
            Log($"resourceSet: {_resourceSet != null}");
            Log($"uniformBuffer: {_uniformBuffer != null}");
            return;
        }

        // Criar estrutura para as matrizes
        var ubo = new UniformBufferObject
        {
            Projection = projectionMatrix,
            View = viewMatrix,
            World = _worldMatrix
        };

        // Atualiza o buffer de uniformes com as matrizes
        _graphicsDevice.UpdateBuffer(_uniformBuffer, 0, ubo);

        // Define o pipeline
        commandList.SetPipeline(_pipeline);

        // Define os buffers de vértices e índices
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

        // Define o conjunto de recursos
        commandList.SetGraphicsResourceSet(0, _resourceSet);

        // Desenha o cubo
        commandList.DrawIndexed(
            36, // 12 triângulos * 3 vértices
            1,
            0,
            0,
            0);

        Log("Cube.Render() completed");
    }

    public override void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _resourceSet?.Dispose();
        _resourceLayout?.Dispose();

        if (_shaders != null)
            foreach (var shader in _shaders)
                shader?.Dispose();

        _pipeline?.Dispose();
    }

    private static void Log(string message)
    {
        // Lazy initialization of log file
        if (_logFile == null)
        {
            _logFile = new StreamWriter("/home/maikeu/MeusProgramas/TestQwen/cube_debug.log", false);
            _logFile.AutoFlush = true;
        }

        _logFile.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}

// Estrutura para os vértices com posição e cor
public struct VertexPositionColor
{
    public Vector3 Position;
    public RgbaFloat Color;

    public VertexPositionColor(Vector3 position, RgbaFloat color)
    {
        Position = position;
        Color = color;
    }

    public static readonly uint SizeInBytes = (3 + 4) * 4; // 3 floats para posição + 4 floats para cor
}