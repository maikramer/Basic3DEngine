using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Basic3DEngine.Rendering;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Entities;

/// <summary>
/// Componente de renderização de cubo com suporte a iluminação
/// </summary>
public class CubeRenderComponentLit : RenderComponent, IShadowCaster
{
    private DeviceBuffer? _vertexBuffer;
    private DeviceBuffer? _indexBuffer;
    private DeviceBuffer? _uniformBuffer;
    private DeviceBuffer? _lightingBuffer;
    private Pipeline? _pipeline;
    private ResourceSet? _uniformResourceSet;
    private ResourceSet? _lightingResourceSet;
    private Shader[]? _shaders;
    private ResourceLayout? _uniformLayout;
    private ResourceLayout? _lightingLayout;
    
    // Propriedades do material
    public float Shininess { get; set; } = 32f;
    public float SpecularIntensity { get; set; } = 0.3f;
    
    // Debug
    private int _debugLogCount = 0;
    
    private readonly LightingSystem _lightingSystem;
    
    public CubeRenderComponentLit(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, RgbaFloat color, LightingSystem lightingSystem)
        : base(graphicsDevice, factory, commandList, color)
    {
        _lightingSystem = lightingSystem;
        CreateResources();
    }
    
    private void CreateResources()
    {
        // Criar geometria do cubo com normais
        CreateCubeGeometry();
        
        // Criar shaders
        CreateShaders();
        
        // Criar pipeline
        CreatePipeline();
        
        // Criar buffers uniformes
        CreateBuffers();
    }
    
    private void CreateCubeGeometry()
    {
        // Vértices do cubo com normais calculadas
        var vertices = new VertexPositionNormalColor[]
        {
            // Face frontal (Z negativo)
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), Color),
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), Color),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1), Color),
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1), Color),
            
            // Face traseira (Z positivo)
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1), Color),
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1), Color),
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1), Color),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1), Color),
            
            // Face esquerda (X negativo)
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-1, 0, 0), Color),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-1, 0, 0), Color),
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-1, 0, 0), Color),
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-1, 0, 0), Color),
            
            // Face direita (X positivo)
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(1, 0, 0), Color),
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(1, 0, 0), Color),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(1, 0, 0), Color),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(1, 0, 0), Color),
            
            // Face inferior (Y negativo)
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0), Color),
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0), Color),
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), Color),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), Color),
            
            // Face superior (Y positivo)
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0), Color),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0), Color),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0), Color),
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0), Color),
        };
        
        // Índices para cada face
        var indices = new ushort[]
        {
            // Face frontal
            0, 1, 2,  0, 2, 3,
            // Face traseira
            4, 5, 6,  4, 6, 7,
            // Face esquerda
            8, 9, 10,  8, 10, 11,
            // Face direita
            12, 13, 14,  12, 14, 15,
            // Face inferior
            16, 17, 18,  16, 18, 19,
            // Face superior
            20, 21, 22,  20, 22, 23,
        };
        
        // Criar buffers
        _vertexBuffer = _factory.CreateBuffer(new BufferDescription(
            (uint)(vertices.Length * VertexPositionNormalColor.GetVertexLayoutDescription().Stride), 
            BufferUsage.VertexBuffer));
        _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);
        
        _indexBuffer = _factory.CreateBuffer(new BufferDescription(
            (uint)(indices.Length * sizeof(ushort)), 
            BufferUsage.IndexBuffer));
        _graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);
    }
    
    private void CreateShaders()
    {
        // Carregar shaders com iluminação
        var basePath = ShaderLoader.GetShadersBasePath();
        var vertexPath = Path.Combine(basePath, "Cube", "cube_lit.vert");
        var fragmentPath = Path.Combine(basePath, "Cube", "cube_lit.frag");
        
        var vertexShader = ShaderLoader.LoadShader(_factory, vertexPath, ShaderStages.Vertex);
        var fragmentShader = ShaderLoader.LoadShader(_factory, fragmentPath, ShaderStages.Fragment);
        
        _shaders = new[] { vertexShader, fragmentShader };
    }
    
    private void CreatePipeline()
    {
        // Layout de recursos para matrizes de transformação
        _uniformLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionViewWorld", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
        
        // Layout de recursos para dados de iluminação
        _lightingLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("LightingData", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
        
        // Descrição do pipeline
        var pipelineDescription = new GraphicsPipelineDescription(
            BlendStateDescription.SingleOverrideBlend,
            DepthStencilStateDescription.DepthOnlyLessEqual,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                new[] { VertexPositionNormalColor.GetVertexLayoutDescription() },
                _shaders),
            new[] { _uniformLayout, _lightingLayout },
            _graphicsDevice.SwapchainFramebuffer.OutputDescription);
        
        _pipeline = _factory.CreateGraphicsPipeline(pipelineDescription);
    }
    
    private void CreateBuffers()
    {
        // Buffer para matrizes de transformação
        _uniformBuffer = _factory.CreateBuffer(new BufferDescription(
            (uint)(3 * 16 * sizeof(float)), // 3 matrizes 4x4
            BufferUsage.UniformBuffer));
        
        // Buffer para dados de iluminação
        _lightingBuffer = _factory.CreateBuffer(new BufferDescription(
            1024, // Buffer grande para dados de luz
            BufferUsage.UniformBuffer));
        
        // Resource sets
        _uniformResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
            _uniformLayout, _uniformBuffer));
        
        _lightingResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
            _lightingLayout, _lightingBuffer));
    }
    
    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (_pipeline == null || _vertexBuffer == null || _indexBuffer == null) return;
        
        // Atualizar matrizes de transformação
        var worldMatrix = Matrix4x4.CreateScale(GameObject?.Scale ?? Vector3.One) *
                         Matrix4x4.CreateFromYawPitchRoll(
                             GameObject?.Rotation.Y ?? 0,
                             GameObject?.Rotation.X ?? 0,
                             GameObject?.Rotation.Z ?? 0) *
                         Matrix4x4.CreateTranslation(GameObject?.Position ?? Vector3.Zero);
        
        var uniformData = new UniformBufferObject
        {
            Projection = projectionMatrix,
            View = viewMatrix,
            World = worldMatrix
        };
        
        commandList.UpdateBuffer(_uniformBuffer, 0, uniformData);
        
        // Atualizar dados de iluminação via uniform buffer
        UpdateLightingData(commandList);
        
        // Renderizar
        commandList.SetPipeline(_pipeline);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.SetGraphicsResourceSet(0, _uniformResourceSet);
        commandList.SetGraphicsResourceSet(1, _lightingResourceSet);
        commandList.DrawIndexed(36, 1, 0, 0, 0); // 12 triângulos * 3 vértices = 36 índices
    }
    
    private void UpdateLightingData(CommandList commandList)
    {
        // Preparar dados de iluminação seguindo exatamente o layout do shader
        var lightingDataList = new List<float>();
        
        // 1. Ambient (16 bytes: vec3 + float)
        lightingDataList.AddRange(new float[] {
            _lightingSystem.AmbientColor.X, _lightingSystem.AmbientColor.Y, _lightingSystem.AmbientColor.Z, _lightingSystem.AmbientIntensity
        });
        
        // 2. Material (16 bytes: 4 floats)
        lightingDataList.AddRange(new float[] {
            Shininess, SpecularIntensity, 0f, 0f
        });
        
        // Contar luzes por tipo
        var directionalLights = _lightingSystem.Lights.Where(l => l.Type == LightType.Directional).Take(4).ToArray();
        var pointLights = _lightingSystem.Lights.Where(l => l.Type == LightType.Point).Take(4).ToArray();
        
        // 3. Contadores de luz (16 bytes: 4 floats)
        lightingDataList.AddRange(new float[] {
            directionalLights.Length, pointLights.Length, 0f, 0f
        });
        
        // 4. Directional lights (32 bytes each: vec4 direction + vec4 color/intensity)
        for (int i = 0; i < 4; i++)
        {
            if (i < directionalLights.Length)
            {
                var light = directionalLights[i];
                // Direction (vec4)
                lightingDataList.AddRange(new float[] {
                    light.Direction.X, light.Direction.Y, light.Direction.Z, 0f
                });
                // Color + intensity (vec4)
                lightingDataList.AddRange(new float[] {
                    light.Color.X, light.Color.Y, light.Color.Z, light.Intensity
                });
            }
            else
            {
                // Preencher com zeros (8 floats = 32 bytes)
                lightingDataList.AddRange(new float[8]);
            }
        }
        
        // 5. Point lights (48 bytes each: vec4 position + vec4 color/intensity + vec4 range/attenuation)
        for (int i = 0; i < 4; i++)
        {
            if (i < pointLights.Length)
            {
                var light = pointLights[i];
                // Position (vec4)
                lightingDataList.AddRange(new float[] {
                    light.Position.X, light.Position.Y, light.Position.Z, 0f
                });
                // Color + intensity (vec4)
                lightingDataList.AddRange(new float[] {
                    light.Color.X, light.Color.Y, light.Color.Z, light.Intensity
                });
                // Range + attenuation + padding (vec4)
                lightingDataList.AddRange(new float[] {
                    light.Range, light.Attenuation, 0f, 0f
                });
            }
            else
            {
                // Preencher com zeros (12 floats = 48 bytes)
                lightingDataList.AddRange(new float[12]);
            }
        }
        
        var lightingData = lightingDataList.ToArray();
        commandList.UpdateBuffer(_lightingBuffer, 0, lightingData);
    }
    
    // Implementação da interface IShadowCaster
    public bool CastsShadows => true;
    
    public void RenderShadowGeometry(CommandList commandList, Matrix4x4 worldMatrix)
    {
        if (_vertexBuffer == null || _indexBuffer == null)
            return;
            
        // Bind apenas a geometria (sem pipeline complexo)
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        
        // Renderizar índices do cubo
        commandList.DrawIndexed(36, 1, 0, 0, 0);
    }
    
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _lightingBuffer?.Dispose();
        _pipeline?.Dispose();
        _uniformResourceSet?.Dispose();
        _lightingResourceSet?.Dispose();
        _uniformLayout?.Dispose();
        _lightingLayout?.Dispose();
        
        if (_shaders != null)
        {
            foreach (var shader in _shaders)
                shader?.Dispose();
        }
    }
} 