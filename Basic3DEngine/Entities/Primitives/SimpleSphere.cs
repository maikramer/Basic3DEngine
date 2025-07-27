using System.IO;
using System.Numerics;
using System.Text;
using Veldrid;
using Basic3DEngine.Services;

namespace Basic3DEngine.Entities.Primitives;

public class SimpleSphere : Geometry
{
    private static StreamWriter _logFile;
    private readonly RgbaFloat _color;

    public SimpleSphere(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, Vector3 position,
        RgbaFloat color)
        : base(graphicsDevice, factory, commandList, position)
    {
        _color = color;
        CreateResources();
    }

    private void CreateResources()
    {
        Log("SimpleSphere.CreateResources() called");
        
        // Gerar vértices da esfera (versão simplificada)
        var (vertices, indices) = GenerateSimpleSphere(8, 6); // 8 segmentos horizontais, 6 verticais

        // Criação dos buffers - mesma estrutura do Cube
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

        // Usar exatamente os mesmos shaders do Cube
        Log("Creating shaders");
        CreateShaders();
        Log("Shaders created successfully");

        // Uniform buffer - mesma estrutura do Cube
        try
        {
            Log("Creating uniform buffer");
            _uniformBuffer = _factory.CreateBuffer(new BufferDescription(
                192, // 3 matrizes de 4x4 floats
                BufferUsage.UniformBuffer));
            Log("Uniform buffer created successfully");
        }
        catch (Exception ex)
        {
            Log($"Error creating uniform buffer: {ex.Message}");
            throw;
        }

        // Resource layout - mesmo do Cube
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

        // Resource set - mesmo do Cube
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

        // Pipeline - mesmo do Cube
        Log("Creating pipeline");
        CreatePipeline();
        Log("Pipeline created successfully");
        Log("SimpleSphere.CreateResources() completed");
    }

    private void CreateShaders()
    {
        Log("SimpleSphere.CreateShaders() called");
        
        try
        {
            var shadersBasePath = ShaderLoader.GetShadersBasePath();
            var cubeShaderPath = Path.Combine(shadersBasePath, "Cube");
            
            Log($"Loading cube shaders from: {cubeShaderPath}");
            
            var vertexShader = ShaderLoader.LoadShader(_factory, 
                Path.Combine(cubeShaderPath, "cube.vert"), 
                ShaderStages.Vertex);
                
            var fragmentShader = ShaderLoader.LoadShader(_factory, 
                Path.Combine(cubeShaderPath, "cube.frag"), 
                ShaderStages.Fragment);

            _shaders = new Shader[] { vertexShader, fragmentShader };
            Log("Cube shaders loaded successfully for SimpleSphere");
        }
        catch (Exception ex)
        {
            Log($"Error loading shaders: {ex.Message}");
            throw;
        }
    }

    private void CreatePipeline()
    {
        Log("SimpleSphere.CreatePipeline() called");
        
        // Exatamente o mesmo pipeline do Cube
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

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

    private (VertexPositionColor[], ushort[]) GenerateSimpleSphere(int horizontalSegments, int verticalSegments)
    {
        var vertices = new List<VertexPositionColor>();
        var indices = new List<ushort>();

        // Gerar vértices da esfera (raio 1.0 para ser escalada corretamente)
        for (int v = 0; v <= verticalSegments; v++)
        {
            float phi = (float)(Math.PI * v / verticalSegments);
            float y = (float)Math.Cos(phi); // Raio 1.0
            float radius = (float)Math.Sin(phi); // Raio 1.0

            for (int h = 0; h <= horizontalSegments; h++)
            {
                float theta = (float)(2.0 * Math.PI * h / horizontalSegments);
                float x = radius * (float)Math.Cos(theta);
                float z = radius * (float)Math.Sin(theta);

                vertices.Add(new VertexPositionColor(new Vector3(x, y, z), _color));
            }
        }

        // Gerar índices
        for (int v = 0; v < verticalSegments; v++)
        {
            for (int h = 0; h < horizontalSegments; h++)
            {
                int current = v * (horizontalSegments + 1) + h;
                int next = current + horizontalSegments + 1;

                // Triângulo 1 (ordem corrigida para winding anti-horário)
                indices.Add((ushort)current);
                indices.Add((ushort)next);
                indices.Add((ushort)(current + 1));

                // Triângulo 2 (ordem corrigida para winding anti-horário)
                indices.Add((ushort)(current + 1));
                indices.Add((ushort)next);
                indices.Add((ushort)(next + 1));
            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    public override void Update(float deltaSeconds)
    {
        // Mesma lógica do Cube
        var rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
        var scaleMatrix = Matrix4x4.CreateScale(Scale);
        var translationMatrix = Matrix4x4.CreateTranslation(Position);
        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        Log($"SimpleSphere.Render() called with position: {Position}, scale: {Scale}");

        if (_vertexBuffer == null || _indexBuffer == null || _shaders == null ||
            _pipeline == null || _resourceSet == null || _uniformBuffer == null)
        {
            Log("SimpleSphere.Render() early exit - one or more resources are null");
            return;
        }

        // Mesma estrutura de renderização do Cube
        var ubo = new UniformBufferObject
        {
            Projection = projectionMatrix,
            View = viewMatrix,
            World = _worldMatrix
        };

        _graphicsDevice.UpdateBuffer(_uniformBuffer, 0, ubo);

        commandList.SetPipeline(_pipeline);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.SetGraphicsResourceSet(0, _resourceSet);

        // Renderizar todos os triângulos da esfera
        commandList.DrawIndexed((uint)(_indexBuffer.SizeInBytes / sizeof(ushort)), 1, 0, 0, 0);

        Log("SimpleSphere.Render() completed");
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
        if (_logFile == null)
        {
            _logFile = new StreamWriter("/home/maikeu/MeusProgramas/TestQwen/simple_sphere_debug.log", false);
            _logFile.AutoFlush = true;
        }

        _logFile.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}

// Reutilizar a estrutura do Cube
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