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
        
        // Bloom parameters
        private DeviceBuffer _bloomParamsBuffer;
        private DeviceBuffer _blurParamsBuffer;
        
        // Resource layouts
        private ResourceLayout _bloomExtractLayout;
        private ResourceLayout _bloomBlurLayout;
        
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

        // Sampler padrão para post-processing
        private Sampler _linearClampSampler;
        
        // Configurações HDR
        public float Exposure { get; set; } = 1.0f;        // Mais visível por padrão
        public float BloomThreshold { get; set; } = 0.8f;  // Threshold mais baixo para evidenciar bloom
        public float BloomIntensity { get; set; } = 1.2f;  // Intensidade mais alta
        
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
            // Sampler linear/clamp usado em todas as amostragens
            _linearClampSampler = _factory.CreateSampler(new SamplerDescription(
                SamplerAddressMode.Clamp,
                SamplerAddressMode.Clamp,
                SamplerAddressMode.Clamp,
                SamplerFilter.MinLinear_MagLinear_MipLinear,
                null,
                0, 0, 0, 0,
                SamplerBorderColor.TransparentBlack));
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
            // Criar pipelines HDR + Bloom
            CreateBloomExtractPipeline();
            CreateBloomBlurPipelines();
            CreateToneMappingPipeline();
        }
        
        private void CreateBloomExtractPipeline()
        {
            // Vertex shader simples (reutilizado)
            var vertexCode = @"
#version 330

out vec2 fsin_texCoords;

void main()
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    fsin_texCoords.x = (x + 1.0) * 0.5;
    fsin_texCoords.y = (y + 1.0) * 0.5;
    gl_Position = vec4(x, y, 0, 1);
}";

            // Fragment shader para bright pass extraction
            var fragmentCode = @"
#version 330

in vec2 fsin_texCoords;
out vec4 fsout_color;

uniform sampler2D HDRTexture;

uniform BloomParams {
    float threshold;    // Limiar de brilho (ex: 1.0)
    float intensity;    // Intensidade do bloom (ex: 0.8)
    float padding1;
    float padding2;
};

void main()
{
    vec3 color = texture(HDRTexture, fsin_texCoords).rgb;
    
    // Calcular luminância
    float luminance = dot(color, vec3(0.2126, 0.7152, 0.0722));
    
    // Bright pass com soft threshold
    float factor = max(0.0, luminance - threshold) / max(luminance, 0.001);
    factor = smoothstep(0.0, 1.0, factor);
    
    // Aplicar intensidade e preservar cor
    fsout_color = vec4(color * factor * intensity, 1.0);
}";

            // Criar shaders
            var vertexShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, System.Text.Encoding.UTF8.GetBytes(vertexCode), "main"));
            var fragmentShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(fragmentCode), "main"));

            // Layout de recursos
            _bloomExtractLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("HDRTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("BloomParams", ResourceKind.UniformBuffer, ShaderStages.Fragment)
            ));

            // Pipeline
            _bloomExtractPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), new[] { vertexShader, fragmentShader }),
                new[] { _bloomExtractLayout },
                _bloomFramebuffer1.OutputDescription
            ));

            // Resource sets serão criados em UpdateResourceSets
            vertexShader.Dispose();
            fragmentShader.Dispose();
        }

        private void CreateBloomBlurPipelines()
        {
            // Vertex shader (mesmo usado anteriormente)
            var vertexCode = @"
#version 330

out vec2 fsin_texCoords;

void main()
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    fsin_texCoords.x = (x + 1.0) * 0.5;
    fsin_texCoords.y = (y + 1.0) * 0.5;
    gl_Position = vec4(x, y, 0, 1);
}";

            // Fragment shader para blur horizontal
            var horizontalBlurCode = @"
#version 330

in vec2 fsin_texCoords;
out vec4 fsout_color;

uniform sampler2D InputTexture;

uniform BlurParams {
    float texelSizeX;
    float texelSizeY;
    float padding1;
    float padding2;
};

void main()
{
    vec3 result = vec3(0.0);
    
    // Gaussian weights (9-tap)
    float weights[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);
    
    // Centro
    result += texture(InputTexture, fsin_texCoords).rgb * weights[0];
    
    // Horizontal blur
    for(int i = 1; i < 5; ++i)
    {
        result += texture(InputTexture, fsin_texCoords + vec2(texelSizeX * i, 0.0)).rgb * weights[i];
        result += texture(InputTexture, fsin_texCoords - vec2(texelSizeX * i, 0.0)).rgb * weights[i];
    }
    
    fsout_color = vec4(result, 1.0);
}";

            // Fragment shader para blur vertical
            var verticalBlurCode = @"
#version 330

in vec2 fsin_texCoords;
out vec4 fsout_color;

uniform sampler2D InputTexture;

uniform BlurParams {
    float texelSizeX;
    float texelSizeY;
    float padding1;
    float padding2;
};

void main()
{
    vec3 result = vec3(0.0);
    
    // Gaussian weights (9-tap)
    float weights[5] = float[](0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);
    
    // Centro
    result += texture(InputTexture, fsin_texCoords).rgb * weights[0];
    
    // Vertical blur
    for(int i = 1; i < 5; ++i)
    {
        result += texture(InputTexture, fsin_texCoords + vec2(0.0, texelSizeY * i)).rgb * weights[i];
        result += texture(InputTexture, fsin_texCoords - vec2(0.0, texelSizeY * i)).rgb * weights[i];
    }
    
    fsout_color = vec4(result, 1.0);
}";

            // Criar shaders
            var vertexShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, System.Text.Encoding.UTF8.GetBytes(vertexCode), "main"));
            var horizontalFragmentShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(horizontalBlurCode), "main"));
            var verticalFragmentShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(verticalBlurCode), "main"));

            // Layout de recursos para blur
            _bloomBlurLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InputTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("BlurParams", ResourceKind.UniformBuffer, ShaderStages.Fragment)
            ));

            // Pipeline horizontal blur
            _bloomBlurHorizontalPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), new[] { vertexShader, horizontalFragmentShader }),
                new[] { _bloomBlurLayout },
                _bloomFramebuffer2.OutputDescription
            ));

            // Pipeline vertical blur
            _bloomBlurVerticalPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleDisabled,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), new[] { vertexShader, verticalFragmentShader }),
                new[] { _bloomBlurLayout },
                _bloomFramebuffer1.OutputDescription
            ));

            // Cleanup
            vertexShader.Dispose();
            horizontalFragmentShader.Dispose();
            verticalFragmentShader.Dispose();
        }
        
        private void CreateToneMappingPipeline()
        {
            // Shader de vertex simples para quad de tela cheia (OpenGL)
            var vertexCode = @"
#version 330

out vec2 fsin_texCoords;

void main()
{
    // Gerar quad de tela cheia com 3 vértices (truque OpenGL)
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    fsin_texCoords.x = (x + 1.0) * 0.5;
    fsin_texCoords.y = (y + 1.0) * 0.5;
    gl_Position = vec4(x, y, 0, 1);
}";

            // Shader de fragment para tone mapping ACES + Bloom (OpenGL)
            var fragmentCode = @"
#version 330

in vec2 fsin_texCoords;
out vec4 fsout_color;

uniform sampler2D HDRTexture;
uniform sampler2D BloomTexture;

uniform ToneMappingParams {
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
    vec3 bloomColor = texture(BloomTexture, fsin_texCoords).rgb;
    
    // Combinar HDR + Bloom
    vec3 finalColor = hdrColor + (bloomColor * bloomIntensity);
    
    // Aplicar exposição
    finalColor *= exposure;
    
    // Tone mapping ACES
    vec3 mapped = ACESFilm(finalColor);
    
    // Gamma correction
    mapped = pow(mapped, vec3(1.0/2.2));
    
    fsout_color = vec4(mapped, 1.0);
}";

            // Criar shaders
            var vertexShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, System.Text.Encoding.UTF8.GetBytes(vertexCode), "main"));
            var fragmentShader = _factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(fragmentCode), "main"));

            // Layout de recursos para tone mapping + bloom (OpenGL)
            _toneMappingLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("HDRTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SamplerHdr", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("BloomTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SamplerBloom", ResourceKind.Sampler, ShaderStages.Fragment),
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
            
            // Bloom parameters (bright pass)
            _bloomParamsBuffer = _factory.CreateBuffer(new BufferDescription(
                16, // 4 floats: threshold, intensity, padding1, padding2
                BufferUsage.UniformBuffer));
            
            // Blur parameters
            _blurParamsBuffer = _factory.CreateBuffer(new BufferDescription(
                16, // 4 floats: texelSizeX, texelSizeY, padding1, padding2
                BufferUsage.UniformBuffer));
            
            UpdateToneMappingParams();
            UpdateBloomParams();
            UpdateBlurParams();

            // Resource set para tone mapping + bloom
            _toneMappingResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _toneMappingLayout,
                _hdrColorView,
                _linearClampSampler,
                _bloomView1, // Final bloom result
                _linearClampSampler,
                _toneMappingParamsBuffer));
            
            // Resource set para bloom extraction
            _bloomExtractResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _bloomExtractLayout,
                _hdrColorView,
                _linearClampSampler,
                _bloomParamsBuffer));
            
            // Resource sets para blur
            _bloomBlur1ResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _bloomBlurLayout,
                _bloomView1,
                _linearClampSampler,
                _blurParamsBuffer));
                
            _bloomBlur2ResourceSet = _factory.CreateResourceSet(new ResourceSetDescription(
                _bloomBlurLayout,
                _bloomView2,
                _linearClampSampler,
                _blurParamsBuffer));
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
        
        private void UpdateBloomParams()
        {
            var bloomParams = new float[]
            {
                BloomThreshold,
                BloomIntensity,
                0.0f,
                0.0f
            };
            _graphicsDevice.UpdateBuffer(_bloomParamsBuffer, 0, bloomParams);
        }
        
        private void UpdateBlurParams()
        {
            var bloomWidth = Width / 2;
            var bloomHeight = Height / 2;
            
            var blurParams = new float[]
            {
                1.0f / bloomWidth,  // texelSizeX
                1.0f / bloomHeight, // texelSizeY
                0.0f, // padding1
                0.0f  // padding2
            };
            _graphicsDevice.UpdateBuffer(_blurParamsBuffer, 0, blurParams);
        }

        public Framebuffer GetHDRFramebuffer()
        {
            return _hdrFramebuffer;
        }

        public void ProcessAndPresent(CommandList commandList, Framebuffer targetFramebuffer)
        {
            if (_toneMappingPipeline == null || _toneMappingResourceSet == null)
                return;

            // **BLOOM PIPELINE COMPLETO**
            
            // 1. BLOOM EXTRACTION - Extrair pixels brilhantes
            commandList.SetFramebuffer(_bloomFramebuffer1);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.SetFullViewports();
            
            commandList.SetPipeline(_bloomExtractPipeline);
            commandList.SetGraphicsResourceSet(0, _bloomExtractResourceSet);
            commandList.Draw(3, 1, 0, 0);
            
            // 2. HORIZONTAL BLUR - Blur horizontal
            commandList.SetFramebuffer(_bloomFramebuffer2);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.SetFullViewports();
            
            commandList.SetPipeline(_bloomBlurHorizontalPipeline);
            commandList.SetGraphicsResourceSet(0, _bloomBlur1ResourceSet);
            commandList.Draw(3, 1, 0, 0);
            
            // 3. VERTICAL BLUR - Blur vertical (resultado final em _bloomFramebuffer1)
            commandList.SetFramebuffer(_bloomFramebuffer1);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.SetFullViewports();
            
            commandList.SetPipeline(_bloomBlurVerticalPipeline);
            commandList.SetGraphicsResourceSet(0, _bloomBlur2ResourceSet);
            commandList.Draw(3, 1, 0, 0);

            // 4. TONE MAPPING FINAL - Combinar HDR + Bloom
            UpdateToneMappingParams();
            
            commandList.SetFramebuffer(targetFramebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.SetFullViewports();
            
            commandList.SetPipeline(_toneMappingPipeline);
            commandList.SetGraphicsResourceSet(0, _toneMappingResourceSet);
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
            _linearClampSampler?.Dispose();
            
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