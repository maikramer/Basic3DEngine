using BepuPhysics.Constraints;

namespace Basic3DEngine.Physics;

/// <summary>
/// Material físico que define as propriedades de interação entre corpos
/// </summary>
public struct Material
{
    /// <summary>
    /// Configurações de mola para resposta de colisão
    /// </summary>
    public SpringSettings SpringSettings;
    
    /// <summary>
    /// Coeficiente de atrito (mantido para compatibilidade, mapeia para DynamicFriction)
    /// </summary>
    public float FrictionCoefficient 
    { 
        get => DynamicFriction;
        set => DynamicFriction = value;
    }
    
    /// <summary>
    /// Velocidade máxima de recuperação após colisão
    /// </summary>
    public float MaximumRecoveryVelocity;
    
    /// <summary>
    /// Coeficiente de atrito estático (quando objetos estão parados)
    /// </summary>
    public float StaticFriction;
    
    /// <summary>
    /// Coeficiente de atrito dinâmico (quando objetos estão em movimento)
    /// </summary>
    public float DynamicFriction;
    
    /// <summary>
    /// Coeficiente de restituição (elasticidade). 0 = perfeitamente inelástico, 1 = perfeitamente elástico
    /// </summary>
    public float Restitution;
    
    /// <summary>
    /// Densidade do material em kg/m³
    /// </summary>
    public float Density;
    
    /// <summary>
    /// Amortecimento aplicado durante contatos
    /// </summary>
    public float ContactDamping;

    /// <summary>
    /// Construtor completo com todas as propriedades
    /// </summary>
    public Material(
        SpringSettings springSettings, 
        float staticFriction, 
        float dynamicFriction, 
        float restitution, 
        float density,
        float maximumRecoveryVelocity = 2f,
        float contactDamping = 0.1f)
    {
        SpringSettings = springSettings;
        StaticFriction = Math.Clamp(staticFriction, 0f, 10f);
        DynamicFriction = Math.Clamp(dynamicFriction, 0f, 10f);
        Restitution = Math.Clamp(restitution, 0f, 1f);
        Density = Math.Max(0.1f, density);
        MaximumRecoveryVelocity = maximumRecoveryVelocity;
        ContactDamping = Math.Clamp(contactDamping, 0f, 1f);
    }

    /// <summary>
    /// Construtor de compatibilidade (mantém assinatura antiga)
    /// </summary>
    public Material(SpringSettings springSettings, float frictionCoefficient = 1f, float maximumRecoveryVelocity = 2f)
        : this(springSettings, frictionCoefficient * 1.2f, frictionCoefficient, 0.2f, 1000f, maximumRecoveryVelocity, 0.1f)
    {
    }

    // Materiais pré-definidos existentes (atualizados com novos valores)
    
    /// <summary>
    /// Madeira - material orgânico com atrito médio e baixa elasticidade
    /// </summary>
    public static Material Wood => new(
        new SpringSettings(15, 0.5f), 
        staticFriction: 0.4f, 
        dynamicFriction: 0.3f, 
        restitution: 0.1f, 
        density: 700f, // kg/m³ (carvalho)
        maximumRecoveryVelocity: 1.5f,
        contactDamping: 0.2f);
    
    /// <summary>
    /// Metal - material rígido com baixo atrito e média elasticidade
    /// </summary>
    public static Material Metal => new(
        new SpringSettings(30, 0.1f), 
        staticFriction: 0.25f, 
        dynamicFriction: 0.2f, 
        restitution: 0.4f, 
        density: 7850f, // kg/m³ (aço)
        maximumRecoveryVelocity: 3f,
        contactDamping: 0.05f);
    
    /// <summary>
    /// Borracha - material flexível com alto atrito e baixa elasticidade
    /// </summary>
    public static Material Rubber => new(
        new SpringSettings(10, 1.5f), 
        staticFriction: 1.0f, 
        dynamicFriction: 0.8f, 
        restitution: 0.3f, 
        density: 1200f, // kg/m³
        maximumRecoveryVelocity: 0.5f,
        contactDamping: 0.3f);
    
    /// <summary>
    /// Gelo - material escorregadio com atrito muito baixo
    /// </summary>
    public static Material Ice => new(
        new SpringSettings(20, 0.05f), 
        staticFriction: 0.08f, 
        dynamicFriction: 0.05f, 
        restitution: 0.1f, 
        density: 917f, // kg/m³
        maximumRecoveryVelocity: 2.5f,
        contactDamping: 0.02f);
    
    /// <summary>
    /// Material padrão genérico
    /// </summary>
    public static Material Default => new(
        new SpringSettings(30, 1),
        staticFriction: 0.6f,
        dynamicFriction: 0.5f,
        restitution: 0.2f,
        density: 1000f); // kg/m³ (água como referência)

    // Novos materiais pré-definidos
    
    /// <summary>
    /// Borracha saltitante - alta elasticidade para objetos que quicam
    /// </summary>
    public static Material RubberBouncy => new(
        new SpringSettings(8, 2f), 
        staticFriction: 1.2f, 
        dynamicFriction: 1.0f, 
        restitution: 0.9f, // muito elástico
        density: 1100f, // kg/m³
        maximumRecoveryVelocity: 5f,
        contactDamping: 0.05f);
    
    /// <summary>
    /// Metal polido - superfície lisa com baixo atrito
    /// </summary>
    public static Material MetalSmooth => new(
        new SpringSettings(35, 0.05f), 
        staticFriction: 0.15f, 
        dynamicFriction: 0.1f, 
        restitution: 0.5f,
        density: 8000f, // kg/m³ (aço inox)
        maximumRecoveryVelocity: 4f,
        contactDamping: 0.02f);
    
    /// <summary>
    /// Madeira áspera - superfície rugosa com alto atrito
    /// </summary>
    public static Material WoodRough => new(
        new SpringSettings(12, 0.8f), 
        staticFriction: 0.7f, 
        dynamicFriction: 0.6f, 
        restitution: 0.05f, // quase inelástico
        density: 800f, // kg/m³ (pinho)
        maximumRecoveryVelocity: 1f,
        contactDamping: 0.3f);
    
    /// <summary>
    /// Gelo escorregadio - atrito extremamente baixo
    /// </summary>
    public static Material IceSlippery => new(
        new SpringSettings(25, 0.02f), 
        staticFriction: 0.03f, 
        dynamicFriction: 0.02f, 
        restitution: 0.05f,
        density: 900f, // kg/m³ (gelo menos denso)
        maximumRecoveryVelocity: 3f,
        contactDamping: 0.01f);
    
    /// <summary>
    /// Vidro - superfície lisa e quebradiça
    /// </summary>
    public static Material Glass => new(
        new SpringSettings(40, 0.01f), 
        staticFriction: 0.4f, 
        dynamicFriction: 0.3f, 
        restitution: 0.6f,
        density: 2500f, // kg/m³
        maximumRecoveryVelocity: 2f,
        contactDamping: 0.01f);
    
    /// <summary>
    /// Concreto - material pesado e áspero
    /// </summary>
    public static Material Concrete => new(
        new SpringSettings(50, 0.1f), 
        staticFriction: 0.9f, 
        dynamicFriction: 0.8f, 
        restitution: 0.02f, // muito inelástico
        density: 2400f, // kg/m³
        maximumRecoveryVelocity: 0.5f,
        contactDamping: 0.4f);
}