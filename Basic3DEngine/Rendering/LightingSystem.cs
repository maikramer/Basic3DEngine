using System.Numerics;
using Veldrid;
using Basic3DEngine.Services;

namespace Basic3DEngine.Rendering;

/// <summary>
/// Tipos de luz disponíveis
/// </summary>
public enum LightType
{
    Directional,    // Luz direcional (como sol)
    Point,          // Luz pontual
    Spot            // Luz de holofote (futuro)
}

/// <summary>
/// Estrutura para dados de luz
/// </summary>
public struct LightData
{
    public LightType Type;
    public Vector3 Position;      // Para luzes pontuais
    public Vector3 Direction;     // Para luzes direcionais
    public Vector3 Color;         // RGB da luz
    public float Intensity;       // Intensidade da luz
    public float Range;           // Alcance para luzes pontuais
    public float Attenuation;     // Atenuação para luzes pontuais
    
    // Luz direcional (sol)
    public static LightData CreateDirectional(Vector3 direction, Vector3 color, float intensity = 1f)
    {
        return new LightData
        {
            Type = LightType.Directional,
            Direction = Vector3.Normalize(direction),
            Color = color,
            Intensity = intensity,
            Position = Vector3.Zero,
            Range = 0f,
            Attenuation = 0f
        };
    }
    
    // Luz pontual
    public static LightData CreatePoint(Vector3 position, Vector3 color, float intensity = 1f, float range = 10f, float attenuation = 1f)
    {
        return new LightData
        {
            Type = LightType.Point,
            Position = position,
            Color = color,
            Intensity = intensity,
            Range = range,
            Attenuation = attenuation,
            Direction = Vector3.Zero
        };
    }
}

/// <summary>
/// Sistema de gerenciamento de iluminação
/// </summary>
public class LightingSystem : IDisposable
{
    private readonly List<LightData> _lights = new();
    private Vector3 _ambientColor = new(0.2f, 0.2f, 0.3f); // Luz ambiente azulada suave
    private float _ambientIntensity = 0.3f;
    
    // Shadow mapping
    private ShadowMapRenderer? _shadowMapRenderer;
    private bool _shadowsEnabled = true; // Reabilitado!
    private Vector3 _sceneCenter = Vector3.Zero;
    
    // Limites do sistema
    public const int MaxLights = 8; // Máximo de luzes simultâneas
    
    /// <summary>
    /// Cor da luz ambiente
    /// </summary>
    public Vector3 AmbientColor 
    { 
        get => _ambientColor; 
        set => _ambientColor = value; 
    }
    
    /// <summary>
    /// Intensidade da luz ambiente
    /// </summary>
    public float AmbientIntensity 
    { 
        get => _ambientIntensity; 
        set => _ambientIntensity = MathF.Max(0f, value); 
    }
    
    /// <summary>
    /// Lista atual de luzes
    /// </summary>
    public IReadOnlyList<LightData> Lights => _lights.AsReadOnly();
    
    /// <summary>
    /// Sombras habilitadas
    /// </summary>
    public bool ShadowsEnabled 
    { 
        get => _shadowsEnabled; 
        set => _shadowsEnabled = value; 
    }
    
    /// <summary>
    /// Centro da cena para cálculo de sombras
    /// </summary>
    public Vector3 SceneCenter 
    { 
        get => _sceneCenter; 
        set => _sceneCenter = value; 
    }

    /// <summary>
    /// Atualiza o centro da cena com base na posição da câmera para melhorar a estabilidade das sombras.
    /// </summary>
    public void UpdateSceneCenter(Vector3 cameraPosition)
    {
        _sceneCenter = cameraPosition;
    }
    
    /// <summary>
    /// Acesso ao renderer de shadow maps
    /// </summary>
    public ShadowMapRenderer? ShadowRenderer => _shadowMapRenderer;
    
    /// <summary>
    /// Adiciona uma luz ao sistema
    /// </summary>
    public bool AddLight(LightData light)
    {
        if (_lights.Count >= MaxLights)
        {
            LoggingService.LogWarning($"Maximum number of lights ({MaxLights}) reached. Cannot add more lights.");
            return false;
        }
        
        _lights.Add(light);
        LoggingService.LogInfo($"Added {light.Type} light. Total lights: {_lights.Count}");
        return true;
    }
    
    /// <summary>
    /// Remove uma luz específica
    /// </summary>
    public bool RemoveLight(int index)
    {
        if (index < 0 || index >= _lights.Count)
            return false;
            
        var removedLight = _lights[index];
        _lights.RemoveAt(index);
        LoggingService.LogInfo($"Removed {removedLight.Type} light. Total lights: {_lights.Count}");
        return true;
    }
    
    /// <summary>
    /// Remove todas as luzes
    /// </summary>
    public void ClearLights()
    {
        var count = _lights.Count;
        _lights.Clear();
        LoggingService.LogInfo($"Cleared all {count} lights");
    }
    
    /// <summary>
    /// Atualiza uma luz existente
    /// </summary>
    public bool UpdateLight(int index, LightData newLight)
    {
        if (index < 0 || index >= _lights.Count)
            return false;
            
        _lights[index] = newLight;
        return true;
    }
    
    /// <summary>
    /// Configura uma cena padrão com iluminação básica
    /// </summary>
    public void SetupDefaultLighting()
    {
        ClearLights();
        
        // ☀️ SOL PRINCIPAL - Luz solar direta forte
        var sunLight = LightData.CreateDirectional(
            new Vector3(-0.3f, -0.8f, -0.2f), // Ângulo solar realista
            new Vector3(1f, 0.95f, 0.85f),    // Cor solar quente
            2.5f                               // Intensidade forte do sol
        );
        AddLight(sunLight);
        
        // 🌤️ SKYLIGHT - Simula luz difusa do céu (hemisférica)
        var skyLight = LightData.CreateDirectional(
            new Vector3(0f, -1f, 0f),         // Diretamente de cima
            new Vector3(0.7f, 0.8f, 1f),      // Azul do céu
            1.2f                               // Intensidade da luz do céu
        );
        AddLight(skyLight);
        
        // 🌍 BOUNCE LIGHT - Simula luz refletida do solo/objetos
        var bounceLight = LightData.CreateDirectional(
            new Vector3(0f, 0.8f, 0f),        // De baixo para cima
            new Vector3(0.9f, 0.85f, 0.7f),   // Cor quente refletida
            0.6f                               // Intensidade moderada
        );
        AddLight(bounceLight);
        
        // 🌅 HORIZON LIGHT - Luz do horizonte para suavizar bordas
        var horizonLight = LightData.CreateDirectional(
            new Vector3(1f, -0.1f, 0f),       // Horizontal, ligeiramente para baixo
            new Vector3(1f, 0.9f, 0.8f),      // Cor quente do horizonte
            0.8f                               // Intensidade suave
        );
        AddLight(horizonLight);
        
        // 🌫️ AMBIENTE GLOBAL - Simula Global Illumination
        AmbientColor = new Vector3(0.4f, 0.45f, 0.5f);   // Azul-acinzentado do céu
        AmbientIntensity = 0.18f;                         // Mais baixo para que as sombras fiquem visíveis
        
        LoggingService.LogInfo("Default lighting setup complete: Simulated Global Illumination (Sun + Sky + Bounce + Horizon + Ambient)");
    }
    
    /// <summary>
    /// Configura iluminação para uma cena noturna
    /// </summary>
    public void SetupNightLighting()
    {
        ClearLights();
        
        // Lua (luz direcional fraca)
        var moonLight = LightData.CreateDirectional(
            new Vector3(0.2f, -0.8f, 0.1f),
            new Vector3(0.8f, 0.9f, 1f),      // Cor azul prateada
            0.3f
        );
        AddLight(moonLight);
        
        // Luzes pontuais espalhadas (como postes de luz)
        var streetLight1 = LightData.CreatePoint(
            new Vector3(-8f, 6f, -5f),
            new Vector3(1f, 0.8f, 0.5f),      // Amarelo quente
            0.6f, 12f, 0.8f
        );
        AddLight(streetLight1);
        
        var streetLight2 = LightData.CreatePoint(
            new Vector3(8f, 6f, 5f),
            new Vector3(1f, 0.8f, 0.5f),
            0.6f, 12f, 0.8f
        );
        AddLight(streetLight2);
        
        // Ambiente noturno mais escuro
        AmbientColor = new Vector3(0.05f, 0.08f, 0.15f);
        AmbientIntensity = 0.15f;
        
        LoggingService.LogInfo("Night lighting setup complete");
    }
    
    /// <summary>
    /// Adiciona uma luz dinâmica na posição da câmera (lanterna)
    /// </summary>
    public void AddFlashlight(Vector3 position, Vector3 direction, Vector3 color, float intensity = 0.7f)
    {
        // Por enquanto, implementar como luz pontual
        // No futuro, pode ser expandido para spotlight
        var flashlight = LightData.CreatePoint(
            position,
            color,
            intensity,
            15f,  // Alcance médio
            1.2f  // Atenuação mais forte
        );
        
        AddLight(flashlight);
        LoggingService.LogInfo("Flashlight added to lighting system");
    }
    
    /// <summary>
    /// Inicializa o sistema de shadow mapping
    /// </summary>
    public void InitializeShadowMapping(GraphicsDevice graphicsDevice, ResourceFactory factory)
    {
        if (!_shadowsEnabled)
        {
            LoggingService.LogInfo("Shadow mapping disabled - skipping initialization");
            return;
        }
        
        _shadowMapRenderer?.Dispose();
        _shadowMapRenderer = new ShadowMapRenderer(graphicsDevice, factory);
        LoggingService.LogInfo("Shadow mapping system initialized");
    }
    
    // Cache para evitar renderizar shadow maps múltiplas vezes por frame
    private uint _lastShadowFrame = 0;
    
    /// <summary>
    /// Renderiza shadow maps para todas as luzes que projetam sombras (apenas uma vez por frame)
    /// </summary>
    public void RenderShadowMaps(CommandList commandList, List<Basic3DEngine.Entities.GameObject> shadowCasters, uint frameNumber)
    {
        if (!_shadowsEnabled || _shadowMapRenderer == null)
            return;
            
        // Evitar renderizar shadow maps múltiplas vezes no mesmo frame
        if (_lastShadowFrame == frameNumber)
            return;
            
        _lastShadowFrame = frameNumber;
            
        // Renderizar shadow map da primeira luz direcional
        var directionalLight = _lights.FirstOrDefault(l => l.Type == LightType.Directional);
        if (directionalLight.Type == LightType.Directional)
        {
            _shadowMapRenderer.RenderShadowMap(commandList, directionalLight, _sceneCenter, shadowCasters);
        }
    }
    
    /// <summary>
    /// Obtém a matrix de sombra da primeira luz direcional
    /// </summary>
    public Matrix4x4 GetMainShadowMatrix()
    {
        if (_shadowMapRenderer == null)
            return Matrix4x4.Identity;
            
        var directionalLight = _lights.FirstOrDefault(l => l.Type == LightType.Directional);
        if (directionalLight.Type == LightType.Directional)
        {
            return _shadowMapRenderer.GetShadowMatrix(directionalLight, _sceneCenter);
        }
        
        return Matrix4x4.Identity;
    }
    
    public void Dispose()
    {
        _shadowMapRenderer?.Dispose();
    }
} 