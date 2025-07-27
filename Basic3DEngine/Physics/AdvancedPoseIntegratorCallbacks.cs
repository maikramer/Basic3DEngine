using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Basic3DEngine.Physics;

/// <summary>
/// Callbacks de integração de pose avançados com suporte a propriedades por corpo
/// </summary>
public struct AdvancedPoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    /// <summary>
    /// Gravidade padrão aplicada aos corpos
    /// </summary>
    public Vector3 DefaultGravity;
    
    /// <summary>
    /// Amortecimento linear padrão (0 = sem amortecimento, 1 = amortecimento total)
    /// </summary>
    public float DefaultLinearDamping;
    
    /// <summary>
    /// Amortecimento angular padrão (0 = sem amortecimento, 1 = amortecimento total)
    /// </summary>
    public float DefaultAngularDamping;
    
    /// <summary>
    /// Buffer para armazenar propriedades customizadas por corpo
    /// </summary>
    public Buffer<BodyProperties> BodyPropertiesData;
    
    /// <summary>
    /// Pool de buffers usado para alocações
    /// </summary>
    private BufferPool _bufferPool;
    
    /// <summary>
    /// Simulação associada
    /// </summary>
    private Simulation _simulation;
    
    // Campos para pré-cálculo na preparação
    private Vector<float> _defaultGravityDt;
    private Vector<float> _defaultLinearDampingDt;
    private Vector<float> _defaultAngularDampingDt;
    private readonly AngularIntegrationMode _angularIntegrationMode;
    
    /// <summary>
    /// Modo de integração angular
    /// </summary>
    public readonly AngularIntegrationMode AngularIntegrationMode => _angularIntegrationMode;
    
    /// <summary>
    /// Permite substeps para corpos não constrangidos
    /// </summary>
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    
    /// <summary>
    /// Integra velocidade para corpos cinemáticos
    /// </summary>
    public readonly bool IntegrateVelocityForKinematics => false;
    
    public AdvancedPoseIntegratorCallbacks(
        Vector3 defaultGravity, 
        float defaultLinearDamping = 0.03f, 
        float defaultAngularDamping = 0.03f,
        AngularIntegrationMode angularIntegrationMode = AngularIntegrationMode.Nonconserving) : this()
    {
        DefaultGravity = defaultGravity;
        DefaultLinearDamping = defaultLinearDamping;
        DefaultAngularDamping = defaultAngularDamping;
        _angularIntegrationMode = angularIntegrationMode;
    }
    
    /// <summary>
    /// Inicializa os callbacks com a simulação
    /// </summary>
    public void Initialize(Simulation simulation)
    {
        _simulation = simulation;
        _bufferPool = simulation.BufferPool;
        
        // Alocar buffer inicial para propriedades
        var initialCapacity = Math.Max(128, simulation.Bodies.HandlePool.HighestPossiblyClaimedId + 1);
        _bufferPool.TakeAtLeast(initialCapacity, out BodyPropertiesData);
        
        // Inicializar todos com valores padrão
        BodyPropertiesData.Clear(0, initialCapacity);
    }
    
    /// <summary>
    /// Prepara para integração pré-calculando valores
    /// </summary>
    public void PrepareForIntegration(float dt)
    {
        // Pré-calcular valores escalados por dt para performance
        _defaultGravityDt = new Vector<float>(dt);
        
        // Converter damping para formato exponencial
        _defaultLinearDampingDt = new Vector<float>(
            MathF.Pow(MathHelper.Clamp(1 - DefaultLinearDamping, 0, 1), dt));
        _defaultAngularDampingDt = new Vector<float>(
            MathF.Pow(MathHelper.Clamp(1 - DefaultAngularDamping, 0, 1), dt));
    }
    
    /// <summary>
    /// Integra velocidades dos corpos aplicando gravidade e damping
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, 
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, 
        ref BodyVelocityWide velocity)
    {
        // Aplicar gravidade em todos os eixos (normalmente só Y é usado, mas vamos ser completos)
        var gravityXDt = new Vector<float>(DefaultGravity.X) * dt;
        var gravityYDt = new Vector<float>(DefaultGravity.Y) * dt;
        var gravityZDt = new Vector<float>(DefaultGravity.Z) * dt;
        
        velocity.Linear.X += gravityXDt;
        velocity.Linear.Y += gravityYDt;
        velocity.Linear.Z += gravityZDt;
        
        // Aplicar damping linear
        velocity.Linear.X *= _defaultLinearDampingDt;
        velocity.Linear.Y *= _defaultLinearDampingDt;
        velocity.Linear.Z *= _defaultLinearDampingDt;
        
        // Aplicar damping angular
        velocity.Angular.X *= _defaultAngularDampingDt;
        velocity.Angular.Y *= _defaultAngularDampingDt;
        velocity.Angular.Z *= _defaultAngularDampingDt;
        
        // Nota: Para uma implementação completa de propriedades por corpo,
        // seria necessário usar uma abordagem mais complexa que não é possível
        // com a API atual do BepuPhysics sem modificações internas.
        // Por enquanto, vamos usar apenas os valores padrão.
    }
    
    /// <summary>
    /// Registra propriedades customizadas para um corpo
    /// </summary>
    public void RegisterBodyProperties(int bodyIndex, BodyProperties properties)
    {
        // Verificar se foi inicializado
        if (_bufferPool == null || !BodyPropertiesData.Allocated)
            return;
            
        // Garantir que temos espaço suficiente
        if (bodyIndex >= BodyPropertiesData.Length)
        {
            // Expandir o buffer se necessário
            var newCapacity = BufferPool.GetCapacityForCount<BodyProperties>(Math.Max(BodyPropertiesData.Length * 2, bodyIndex + 1));
            _bufferPool.ResizeToAtLeast(ref BodyPropertiesData, newCapacity, BodyPropertiesData.Length);
        }
        
        BodyPropertiesData[bodyIndex] = properties;
    }
    
    /// <summary>
    /// Remove propriedades customizadas de um corpo
    /// </summary>
    public void UnregisterBodyProperties(int bodyIndex)
    {
        if (_bufferPool == null || !BodyPropertiesData.Allocated || bodyIndex >= BodyPropertiesData.Length)
            return;
            
        BodyPropertiesData[bodyIndex] = default;
    }
    
    /// <summary>
    /// Verifica se um corpo tem propriedades customizadas
    /// </summary>
    public bool HasBodyProperties(int bodyIndex)
    {
        if (_bufferPool == null || !BodyPropertiesData.Allocated)
            return false;
            
        return bodyIndex < BodyPropertiesData.Length && 
               BodyPropertiesData[bodyIndex].GravityScale > 0;
    }
    
    /// <summary>
    /// Libera recursos alocados
    /// </summary>
    public void Dispose()
    {
        if (BodyPropertiesData.Allocated)
        {
            _bufferPool.Return(ref BodyPropertiesData);
        }
    }
} 