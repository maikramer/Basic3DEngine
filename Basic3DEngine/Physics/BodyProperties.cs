namespace Basic3DEngine.Physics;

/// <summary>
/// Propriedades físicas individuais por corpo para simulação avançada
/// </summary>
/// <remarks>
/// Esta estrutura permite customização detalhada do comportamento físico de cada corpo,
/// incluindo amortecimento, limites de velocidade e escala de gravidade.
/// </remarks>
public record struct BodyProperties
{
    /// <summary>
    /// Fração da velocidade linear a remover por unidade de tempo.
    /// Valores de 0 a 1, onde 0 é sem amortecimento e valores próximos a 1 removem a maior parte da velocidade.
    /// </summary>
    public float LinearDamping { get; init; }
    
    /// <summary>
    /// Fração da velocidade angular a remover por unidade de tempo.
    /// Valores de 0 a 1, onde 0 é sem amortecimento e valores próximos a 1 removem a maior parte da velocidade.
    /// </summary>
    public float AngularDamping { get; init; }
    
    /// <summary>
    /// Velocidade linear máxima permitida para o corpo em unidades por segundo.
    /// Útil para prevenir velocidades irrealistas ou instabilidades numéricas.
    /// </summary>
    public float MaxLinearVelocity { get; init; }
    
    /// <summary>
    /// Velocidade angular máxima permitida para o corpo em radianos por segundo.
    /// Útil para prevenir rotações excessivas ou instabilidades numéricas.
    /// </summary>
    public float MaxAngularVelocity { get; init; }
    
    /// <summary>
    /// Escala de gravidade aplicada a este corpo específico.
    /// 1.0 = gravidade normal, 0.5 = metade da gravidade, 2.0 = dobro da gravidade, 0.0 = sem gravidade.
    /// </summary>
    public float GravityScale { get; init; }
    
    /// <summary>
    /// Cria propriedades de corpo com valores padrão sensatos
    /// </summary>
    public BodyProperties() : this(0.03f, 0.03f, float.MaxValue, float.MaxValue, 1.0f)
    {
    }
    
    /// <summary>
    /// Cria propriedades de corpo com valores específicos
    /// </summary>
    /// <param name="linearDamping">Amortecimento linear (0-1)</param>
    /// <param name="angularDamping">Amortecimento angular (0-1)</param>
    /// <param name="maxLinearVelocity">Velocidade linear máxima</param>
    /// <param name="maxAngularVelocity">Velocidade angular máxima</param>
    /// <param name="gravityScale">Escala de gravidade</param>
    public BodyProperties(
        float linearDamping = 0.03f,
        float angularDamping = 0.03f,
        float maxLinearVelocity = float.MaxValue,
        float maxAngularVelocity = float.MaxValue,
        float gravityScale = 1.0f)
    {
        LinearDamping = Math.Clamp(linearDamping, 0f, 1f);
        AngularDamping = Math.Clamp(angularDamping, 0f, 1f);
        MaxLinearVelocity = Math.Max(0f, maxLinearVelocity);
        MaxAngularVelocity = Math.Max(0f, maxAngularVelocity);
        GravityScale = Math.Max(0f, gravityScale);
    }
    
    /// <summary>
    /// Propriedades padrão sem amortecimento
    /// </summary>
    public static readonly BodyProperties Undamped = new(0f, 0f);
    
    /// <summary>
    /// Propriedades com amortecimento leve (padrão)
    /// </summary>
    public static readonly BodyProperties Default = new();
    
    /// <summary>
    /// Propriedades com amortecimento médio
    /// </summary>
    public static readonly BodyProperties MediumDamping = new(0.1f, 0.1f);
    
    /// <summary>
    /// Propriedades com amortecimento alto (movimento viscoso)
    /// </summary>
    public static readonly BodyProperties HighDamping = new(0.3f, 0.3f);
    
    /// <summary>
    /// Propriedades para objetos no espaço (sem gravidade, sem amortecimento)
    /// </summary>
    public static readonly BodyProperties Space = new(0f, 0f, gravityScale: 0f);
    
    /// <summary>
    /// Propriedades para objetos subaquáticos (alto amortecimento, gravidade reduzida)
    /// </summary>
    public static readonly BodyProperties Underwater = new(0.5f, 0.4f, gravityScale: 0.3f);
} 