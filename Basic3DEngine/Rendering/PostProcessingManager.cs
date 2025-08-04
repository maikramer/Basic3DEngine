using System;
using System.Numerics;
using Veldrid;
using Basic3DEngine.Services;

namespace Basic3DEngine.Rendering
{
    /// <summary>
    /// Gerenciador de post-processing HDR com tone mapping e bloom
    /// </summary>
    public class PostProcessingManager : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ResourceFactory _factory;
        
        // HDR Framebuffer
        private Texture _hdrColorTexture;
        private Texture _hdrDepthTexture;
        private TextureView _hdrColorView;
        private Framebuffer _hdrFramebuffer;
        
        // Tone mapping
        private Pipeline _toneMappingPipeline;
        private ResourceLayout _toneMappingLayout;
        private ResourceSet _toneMappingResourceSet;
        private DeviceBuffer _toneMappingParamsBuffer;
        
        // Bloom
        private Texture _bloomTexture1;
        private Texture _bloomTexture2;
        private TextureView _bloomView1;
        private TextureView _bloomView2;
        private Framebuffer _bloomFramebuffer1;
        private Framebuffer _bloomFramebuffer2;
        private Pipeline _bloomExtractPipeline;
        private Pipeline _bloomBlurHorizontalPipeline;
        private Pipeline _bloomBlurVerticalPipeline;
        private ResourceSet _bloomExtractResourceSet;
        private ResourceSet _bloomBlur1ResourceSet;
        private ResourceSet _bloomBlur2ResourceSet;
        
        // Quad de tela cheia
        private DeviceBuffer _quadVertexBuffer;
        private DeviceBuffer _quadIndexBuffer;
        
        // Configurações HDR
        public float Exposure { get; set; } = 1.0f;
        public float BloomThreshold { get; set; } = 1.0f;
        public float BloomIntensity { get; set; } = 0.3f;
        
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public PostProcessingManager(GraphicsDevice graphicsDevice, ResourceFactory factory, 
            uint width, uint height)
        {
            _graphicsDevice = graphicsDevice;
            _factory = factory;
            Width = width;
            Height = height;
            
            CreateResources();
            LoggingService.LogInfo($"PostProcessingManager initialized - {Width}x{Height} HDR");
        }

        private void CreateResources()
        {
            CreateHDRFramebuffer();
            CreateBloomTextures();
            CreateQuadGeometry();
            CreatePipelines();
            UpdateResourceSets();
        }

        private void CreateHDRFramebuffer()
        {
            // HDR Color Texture (RGBA16_Float para alta precisão)
            _hdrColorTexture = _factory.CreateTexture(new TextureDescription(
                Width, Height, 1, 1, 1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D));
            
            _hdrColorView = _factory.CreateTextureView(_hdrColorTexture);

            // HDR Depth Texture (sem Sampled usage)
            _hdrDepthTexture = _factory.CreateTexture(new TextureDescription(
                Width, Height, 1, 1, 1,
                PixelFormat.D32_Float_S8_UInt,
                TextureUsage.DepthStencil,
                TextureType.Texture2D));

            // HDR Framebuffer
            _hdrFramebuffer = _factory.CreateFramebuffer(new FramebufferDescription(
                _hdrDepthTexture, _hdrColorTexture));
        }

        private void CreateBloomTextures()
        {
            var bloomWidth = Width / 2;
            var bloomHeight = Height / 2;

            // Bloom Textures (metade da resolução)
            _bloomTexture1 = _factory.CreateTexture(new TextureDescription(
                bloomWidth, bloomHeight, 1, 1, 1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D));
            
            _bloomTexture2 = _factory.CreateTexture(new TextureDescription(
                bloomWidth, bloomHeight, 1, 1, 1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D));

            _bloomView1 = _factory.CreateTextureView(_bloomTexture1);
            _bloomView2 = _factory.CreateTextureView(_bloomTexture2);

            _bloomFramebuffer1 = _factory.CreateFramebuffer(new FramebufferDescription(null, _bloomTexture1));
            _bloomFramebuffer2 = _factory.CreateFramebuffer(new FramebufferDescription(null, _bloomTexture2));
        }

        private void CreateQuadGeometry()
        {
            // Quad de tela cheia para post-processing
            var quadVertices = new float[]
            {
                -1f, -1f, 0f, 0f, // Bottom-left
                 1f, -1f, 1f, 0f, // Bottom-right
                 1f,  1f, 1f, 1f, // Top-right
                -1f,  1f, 0f, 1f  // Top-left
            };

            var quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

            _quadVertexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(quadVertices.Length * sizeof(float)), 
                BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_quadVertexBuffer, 0, quadVertices);

            _quadIndexBuffer = _factory.CreateBuffer(new BufferDescription(
                (uint)(quadIndices.Length * sizeof(ushort)), 
                BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_quadIndexBuffer, 0, quadIndices);
        }

        private void CreatePipelines()
        {
            // Criar shaders de tone mapping básico
            CreateToneMappingPipeline();
        }
        
        private void CreateToneMappingPipeline()
        {
            // Shader de vertex simples para quad de tela cheia (OpenGL)
            var vertexCode = @"
#version 450

layout(location = 0) out vec2 fsin_texCoords;

void main()
{
    // Gerar quad de tela cheia com 3 vértices (truque OpenGL)
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    fsin_texCoords.x = (x + 1.0) * 0.5;
    fsin_texCoords.y = (y + 1.0) * 0.5;
    gl_Position = vec4(x, y, 0, 1);
}";

            // Shader de fragment para tone mapping ACES (OpenGL)
            var fragmentCode = @"
#version 450

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(binding = 0) uniform sampler2D HDRTexture;

layout(binding = 1) uniform ToneMappingParams {
    float exposure;
    float bloomThreshold;
    float bloomIntensity;
    float padding;
};

// ACES tone mapping
vec3 ACESFilm(vec3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

void main()
{
    vec3 hdrColor = texture(HDRTexture, fsin_texCoords).rgb;
    
    // Aplicar exposição
    hdrColor *= exposure;
    
    // Tone mapping ACES
    vec3 mapped = ACESFilm(hdrColor);
    
    // Gamma correction
    mapped = pow(mapped, vec3(1.0/2.2));
    
    fsout_color = vec4(mapped, 1.0);
}";

            // Criar shaders
            var vertexShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, System.Text.Encoding.UTF8.GetBytes(vertexCode), "main"));
            var fragmentShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(fragmentCode), "main"));

            // Layout de recursos para tone mapping (OpenGL)
            _toneMappingLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("HDRTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ToneMappingParams", ResourceKind.UniformBuffer, ShaderStages.Fragment)
            ));

            // Pipeline de tone mapping 
            _toneMappingPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), new[] { vertexShader, fragmentShader }),
                new[] { _toneMappingLayout },
                _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription
            ));

            // Limpar shaders temporários
            vertexShader.Dispose();
            fragmentShader.Dispose();
        }

        private void UpdateResourceSets()
        {
            // Tone mapping parameters
            _toneMappingParamsBuffer = _factory.CreateBuffer(new BufferDescription(
                16, // 4 floats: exposure, bloomThreshold, bloomIntensity, padding
                BufferUsage.UniformBuffer));
            
            UpdateToneMappingParams();

            // Resource set para tone mapping (OpenGL)
            _toneMappingResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _toneMappingLayout,
                _hdrColorView,
                _toneMappingParamsBuffer));
        }

        private void UpdateToneMappingParams()
        {
            var toneMappingParams = new float[]
            {
                Exposure,
                BloomThreshold,
                BloomIntensity,
                0f // padding
            };
            
            _graphicsDevice.UpdateBuffer(_toneMappingParamsBuffer, 0, toneMappingParams);
        }

        public Framebuffer GetHDRFramebuffer()
        {
            return _hdrFramebuffer;
        }

        public void ProcessAndPresent(CommandList commandList, Framebuffer targetFramebuffer)
        {
            if (_toneMappingPipeline == null || _toneMappingResourceSet == null)
                return;

            // Atualizar parâmetros de tone mapping
            UpdateToneMappingParams();

            // Renderizar tone mapping para LDR
            commandList.SetFramebuffer(targetFramebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.SetFullViewports();
            
            // Usar pipeline de tone mapping
            commandList.SetPipeline(_toneMappingPipeline);
            commandList.SetGraphicsResourceSet(0, _toneMappingResourceSet);
            
            // Renderizar quad de tela cheia sem vertex buffer (usando gl_VertexIndex)
            commandList.Draw(3, 1, 0, 0);
        }

        public void Resize(uint width, uint height)
        {
            if (Width == width && Height == height)
                return;

            Width = width;
            Height = height;

            // Dispose recursos antigos
            DisposeResources();
            
            // Recriar com novo tamanho
            CreateResources();
            
            LoggingService.LogInfo($"PostProcessingManager resized to {Width}x{Height}");
        }

        private void DisposeResources()
        {
            _hdrColorTexture?.Dispose();
            _hdrDepthTexture?.Dispose();
            _hdrColorView?.Dispose();
            _hdrFramebuffer?.Dispose();
            
            _bloomTexture1?.Dispose();
            _bloomTexture2?.Dispose();
            _bloomView1?.Dispose();
            _bloomView2?.Dispose();
            _bloomFramebuffer1?.Dispose();
            _bloomFramebuffer2?.Dispose();
            
            _toneMappingParamsBuffer?.Dispose();
        }

        public void Dispose()
        {
            DisposeResources();
            
            _quadVertexBuffer?.Dispose();
            _quadIndexBuffer?.Dispose();
            
            _toneMappingPipeline?.Dispose();
            _toneMappingLayout?.Dispose();
            _toneMappingResourceSet?.Dispose();
            
            _bloomExtractPipeline?.Dispose();
            _bloomBlurHorizontalPipeline?.Dispose();
            _bloomBlurVerticalPipeline?.Dispose();
            _bloomExtractResourceSet?.Dispose();
            _bloomBlur1ResourceSet?.Dispose();
            _bloomBlur2ResourceSet?.Dispose();
        }
    }
}