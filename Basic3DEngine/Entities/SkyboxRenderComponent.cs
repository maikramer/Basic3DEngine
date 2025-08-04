using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Basic3DEngine.Services;
using Basic3DEngine.Rendering;
using Basic3DEngine.Core;

namespace Basic3DEngine.Entities
{
    public class SkyboxRenderComponent : RenderComponent, IDisposable
    {
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _timeBuffer;
        private DeviceBuffer _lightBuffer;
        private DeviceBuffer _mvpBuffer;
        
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _layout;
        
        private readonly LightingSystem _lightingSystem;
        
        private float _time = 0f;
        
        // Skybox é um cubo invertido (6 faces, 8 vértices, 36 índices)
        private static readonly Vector3[] SkyboxVertices = new Vector3[]
        {
            // Cubo centrado na origem
            new Vector3(-1, -1, -1), // 0
            new Vector3( 1, -1, -1), // 1
            new Vector3( 1,  1, -1), // 2
            new Vector3(-1,  1, -1), // 3
            new Vector3(-1, -1,  1), // 4
            new Vector3( 1, -1,  1), // 5
            new Vector3( 1,  1,  1), // 6
            new Vector3(-1,  1,  1), // 7
        };
        
        // Índices para faces internas (winding order invertida)
        private static readonly uint[] SkyboxIndices = new uint[]
        {
            // Front (Z+)
            4, 6, 5, 4, 7, 6,
            // Back (Z-)
            1, 2, 0, 0, 2, 3,
            // Left (X-)
            0, 3, 4, 4, 3, 7,
            // Right (X+)
            5, 6, 1, 1, 6, 2,
            // Top (Y+)
            3, 2, 7, 7, 2, 6,
            // Bottom (Y-)
            4, 5, 0, 0, 5, 1
        };

        public SkyboxRenderComponent(GraphicsDevice device, ResourceFactory factory, CommandList commandList, 
            LightingSystem lightingSystem, OutputDescription? hdrOutputDescription = null) 
            : base(device, factory, commandList, RgbaFloat.Clear)
        {
            _lightingSystem = lightingSystem;
            
            CreateResources(hdrOutputDescription);
        }

        private void CreateResources(OutputDescription? hdrOutputDescription = null)
        {
            // Vertex buffer
            _vertexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(SkyboxVertices.Length * sizeof(float) * 3), 
                BufferUsage.VertexBuffer));
            
            var vertexData = new float[SkyboxVertices.Length * 3];
            for (int i = 0; i < SkyboxVertices.Length; i++)
            {
                vertexData[i * 3 + 0] = SkyboxVertices[i].X;
                vertexData[i * 3 + 1] = SkyboxVertices[i].Y;
                vertexData[i * 3 + 2] = SkyboxVertices[i].Z;
            }
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertexData);

            // Index buffer
            _indexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(SkyboxIndices.Length * sizeof(uint)), 
                BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, SkyboxIndices);

            // Time buffer (for animation)
            _timeBuffer = _factory.CreateBuffer(new BufferDescription(
                16, // vec4 - time, windSpeed, cloudScale, sunIntensity
                BufferUsage.UniformBuffer));

            // Light buffer (para posição do sol)
            _lightBuffer = _factory.CreateBuffer(new BufferDescription(
                32, // vec4 sunDirection + vec4 sunColor
                BufferUsage.UniformBuffer));

            // MVP buffer
            _mvpBuffer = _factory.CreateBuffer(new BufferDescription(
                64, // mat4 (16 floats * 4 bytes)
                BufferUsage.UniformBuffer));

            // Shaders
            var shaderPath = ShaderLoader.GetShadersBasePath();
            var vertexShader = ShaderLoader.LoadShader(_factory, 
                Path.Combine(shaderPath, "Skybox", "skybox.vert"), 
                ShaderStages.Vertex);
            var fragmentShader = ShaderLoader.LoadShader(_factory, 
                Path.Combine(shaderPath, "Skybox", "skybox.frag"), 
                ShaderStages.Fragment);

            // Resource layout
            _layout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("TimeData", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightData", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            // Resource set
            _resourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _layout, _mvpBuffer, _timeBuffer, _lightBuffer));

            // Vertex layout
            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            // Pipeline
            _pipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: false, // Skybox não escreve no depth buffer
                    comparisonKind: ComparisonKind.LessEqual), // LessEqual para skybox
                RasterizerStateDescription.CullNone, // Não fazer cull das faces
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[] { vertexLayout },
                    new[] { vertexShader, fragmentShader }),
                new[] { _layout },
                hdrOutputDescription ?? _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription));

            LoggingService.LogInfo("SkyboxRenderComponent initialized with procedural shaders");
        }

        public override void Render(CommandList commandList, Matrix4x4 view, Matrix4x4 projection)
        {
            // Atualizar tempo para animação das nuvens
            _time += Time.DeltaTime;
            
            // Atualizar buffers
            UpdateTimeData(commandList);
            UpdateLightData(commandList);
            
            // Remover translação da view matrix para skybox
            var skyboxView = view;
            skyboxView.M41 = 0; // X translation
            skyboxView.M42 = 0; // Y translation  
            skyboxView.M43 = 0; // Z translation
            
            var mvp = skyboxView * projection;
            
            // Configurar pipeline
            commandList.SetPipeline(_pipeline);
            commandList.SetGraphicsResourceSet(0, _resourceSet);
            
            // Atualizar MVP buffer
            commandList.UpdateBuffer(_mvpBuffer, 0, mvp);
            
            // Configurar vertex/index buffers
            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
            
            // Renderizar
            commandList.DrawIndexed((uint)SkyboxIndices.Length);
        }

        private void UpdateTimeData(CommandList commandList)
        {
            var timeData = new float[]
            {
                _time,           // Tempo total
                0.5f,            // Velocidade do vento (mais rápido)
                1.5f,            // Escala das nuvens (menor = nuvens maiores)
                2.0f             // Intensidade do sol (mais forte)
            };
            commandList.UpdateBuffer(_timeBuffer, 0, timeData);
        }

        private void UpdateLightData(CommandList commandList)
        {
            // Pegar primeira luz direcional como "sol"
            var directionalLights = _lightingSystem.Lights.Where(l => l.Type == LightType.Directional).ToArray();
            
            Vector3 sunDirection = Vector3.UnitY; // Default: sol no topo
            Vector3 sunColor = Vector3.One;        // Default: branco
            
            if (directionalLights.Length > 0)
            {
                var sunLight = directionalLights[0];
                sunDirection = Vector3.Normalize(sunLight.Direction);
                sunColor = sunLight.Color * sunLight.Intensity;
            }
            
            var lightData = new float[]
            {
                sunDirection.X, sunDirection.Y, sunDirection.Z, 0f, // vec4 sunDirection
                sunColor.X, sunColor.Y, sunColor.Z, 1f              // vec4 sunColor
            };
            commandList.UpdateBuffer(_lightBuffer, 0, lightData);
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _timeBuffer?.Dispose();
            _lightBuffer?.Dispose();
            _mvpBuffer?.Dispose();
            _pipeline?.Dispose();
            _resourceSet?.Dispose();
            _layout?.Dispose();
        }
    }
}