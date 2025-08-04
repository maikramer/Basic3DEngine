using System;
using System.Numerics;
using Veldrid;
using Basic3DEngine.Services;
using Basic3DEngine.Entities;

namespace Basic3DEngine.Rendering
{
    /// <summary>
    /// Renderizador de shadow maps para sistema de sombras
    /// </summary>
    public class ShadowMapRenderer : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ResourceFactory _factory;
        
        // Shadow map resources
        private Texture _shadowMapTexture;
        private TextureView _shadowMapView;
        private Framebuffer _shadowFramebuffer;
        
        // Shadow rendering pipeline
        private Pipeline _shadowPipeline;
        private ResourceLayout _shadowLayout;
        private DeviceBuffer _shadowMvpBuffer;
        private ResourceSet _shadowResourceSet;
        
        // Shadow map settings
        public uint ShadowMapSize { get; private set; } = 2048;
        public float ShadowDistance { get; set; } = 50f;
        public float ShadowBias { get; set; } = 0.0005f;
        
        // Debug/logging
        private int _frameCount = 0;
        
        public Texture ShadowMapTexture => _shadowMapTexture;
        public TextureView ShadowMapView => _shadowMapView;

        public ShadowMapRenderer(GraphicsDevice graphicsDevice, ResourceFactory factory)
        {
            _graphicsDevice = graphicsDevice;
            _factory = factory;
            
            CreateShadowMapResources();
            CreateShadowPipeline();
            
            LoggingService.LogInfo($"ShadowMapRenderer initialized - {ShadowMapSize}x{ShadowMapSize}");
        }

        private void CreateShadowMapResources()
        {
            // Criar shadow map texture (depth only)
            _shadowMapTexture = _factory.CreateTexture(new TextureDescription(
                ShadowMapSize, ShadowMapSize, 1, 1, 1,
                PixelFormat.D32_Float_S8_UInt,
                TextureUsage.DepthStencil | TextureUsage.Sampled,
                TextureType.Texture2D));

            _shadowMapView = _factory.CreateTextureView(_shadowMapTexture);

            // Criar framebuffer para shadow rendering
            _shadowFramebuffer = _factory.CreateFramebuffer(new FramebufferDescription(_shadowMapTexture));
            
            // MVP buffer para shadow rendering
            _shadowMvpBuffer = _factory.CreateBuffer(new BufferDescription(
                64, // mat4 (16 floats * 4 bytes)
                BufferUsage.UniformBuffer));
        }

        private void CreateShadowPipeline()
        {
            // Carregar shadow shaders
            var shaderPath = ShaderLoader.GetShadersBasePath();
            var vertexShader = ShaderLoader.LoadShader(_factory, 
                System.IO.Path.Combine(shaderPath, "Shadow", "shadow.vert"), 
                ShaderStages.Vertex);
            var fragmentShader = ShaderLoader.LoadShader(_factory, 
                System.IO.Path.Combine(shaderPath, "Shadow", "shadow.frag"), 
                ShaderStages.Fragment);

            // Resource layout para MVP matrix
            _shadowLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ShadowMVP", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            // Resource set
            _shadowResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _shadowLayout, _shadowMvpBuffer));

            // Vertex layout (apenas position)
            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            // Pipeline para shadow rendering
            _shadowPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.Empty,
                new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.Less),
                RasterizerStateDescription.CullNone, // Não fazer cull para evitar peter panning
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[] { vertexLayout },
                    new[] { vertexShader, fragmentShader }),
                new[] { _shadowLayout },
                _shadowFramebuffer.OutputDescription));
        }

        /// <summary>
        /// Renderiza shadow map para uma luz direcional
        /// </summary>
        public void RenderShadowMap(CommandList commandList, LightData light, Vector3 sceneCenter, 
            List<GameObject> shadowCasters)
        {
            if (light.Type != LightType.Directional)
                return;

            // Calcular light view matrix
            var lightPosition = sceneCenter - (light.Direction * ShadowDistance * 0.5f);
            var lightView = Matrix4x4.CreateLookAt(lightPosition, sceneCenter, Vector3.UnitY);
            
            // Calcular orthographic projection para luz direcional
            var lightProjection = Matrix4x4.CreateOrthographic(
                ShadowDistance, ShadowDistance, 0.1f, ShadowDistance);
            
            var lightMvp = lightView * lightProjection;

            // Setup shadow rendering
            commandList.SetFramebuffer(_shadowFramebuffer);
            commandList.ClearDepthStencil(1f);
            commandList.SetViewport(0, new Viewport(0, 0, ShadowMapSize, ShadowMapSize, 0f, 1f));
            
            commandList.SetPipeline(_shadowPipeline);
            commandList.SetGraphicsResourceSet(0, _shadowResourceSet);
            
            // Update MVP buffer
            commandList.UpdateBuffer(_shadowMvpBuffer, 0, lightMvp);
            
            // Renderizar objetos que fazem sombra
            var shadowCasterCount = 0;
            foreach (var gameObject in shadowCasters)
            {
                if (RenderGameObjectShadow(commandList, gameObject))
                    shadowCasterCount++;
            }
            
            // Log ocasionalmente para debug
            _frameCount++;
            if (_frameCount % 300 == 0 && shadowCasterCount > 0)
            {
                LoggingService.LogInfo($"Shadow mapping active: {shadowCasterCount} shadow casters, {ShadowMapSize}x{ShadowMapSize} map");
            }
        }

        private bool RenderGameObjectShadow(CommandList commandList, GameObject gameObject)
        {
            // Verificar se algum componente implementa IShadowCaster
            foreach (var component in gameObject.GetAllComponents())
            {
                if (component is IShadowCaster shadowCaster && shadowCaster.CastsShadows)
                {
                    // Calcular matrix de transformação do objeto  
                    var rotationMatrix = Matrix4x4.CreateRotationX(gameObject.Rotation.X) *
                                       Matrix4x4.CreateRotationY(gameObject.Rotation.Y) *
                                       Matrix4x4.CreateRotationZ(gameObject.Rotation.Z);
                    
                    var worldMatrix = 
                        Matrix4x4.CreateScale(gameObject.Scale) *
                        rotationMatrix *
                        Matrix4x4.CreateTranslation(gameObject.Position);
                    
                    shadowCaster.RenderShadowGeometry(commandList, worldMatrix);
                    return true; // Renderizou com sucesso
                }
            }
            return false; // Não renderizou nada
        }

        /// <summary>
        /// Calcula a matrix de transformação para sampling da shadow map
        /// </summary>
        public Matrix4x4 GetShadowMatrix(LightData light, Vector3 sceneCenter)
        {
            if (light.Type != LightType.Directional)
                return Matrix4x4.Identity;

            var lightPosition = sceneCenter - (light.Direction * ShadowDistance * 0.5f);
            var lightView = Matrix4x4.CreateLookAt(lightPosition, sceneCenter, Vector3.UnitY);
            var lightProjection = Matrix4x4.CreateOrthographic(
                ShadowDistance, ShadowDistance, 0.1f, ShadowDistance);
            
            // Bias matrix para converter de [-1,1] para [0,1]
            var biasMatrix = new Matrix4x4(
                0.5f, 0.0f, 0.0f, 0.5f,
                0.0f, 0.5f, 0.0f, 0.5f,
                0.0f, 0.0f, 0.5f, 0.5f,
                0.0f, 0.0f, 0.0f, 1.0f);
            
            return lightView * lightProjection * biasMatrix;
        }

        public void Dispose()
        {
            _shadowMapTexture?.Dispose();
            _shadowMapView?.Dispose();
            _shadowFramebuffer?.Dispose();
            _shadowPipeline?.Dispose();
            _shadowLayout?.Dispose();
            _shadowMvpBuffer?.Dispose();
            _shadowResourceSet?.Dispose();
        }
    }
}