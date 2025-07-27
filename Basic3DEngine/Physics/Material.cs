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
        new SpringSettings(20, 1f), // Valores mais conservadores
        staticFriction: 0.4f, 
        dynamicFriction: 0.3f, 
        restitution: 0.1f, 
        density: 700f, // kg/m³ (carvalho)
        maximumRecoveryVelocity: 1f, // Reduzido para estabilidade
        contactDamping: 0.1f); // Reduzido para evitar instabilidade
    
    /// <summary>
    /// Metal - material rígido com baixo atrito e média elasticidade
    /// </summary>
    public static Material Metal => new(
        new SpringSettings(30, 1), // Valores padrão do BepuPhysics
        staticFriction: 1f, 
        dynamicFriction: 1f, 
        restitution: 0.2f, 
        density: 7850f, // kg/m³ (aço)
        maximumRecoveryVelocity: 2f, // Mantido
        contactDamping: 0.0f);
    
    /// <summary>
    /// Borracha - material flexível com alto atrito e baixa elasticidade
    /// </summary>
    public static Material Rubber => new(
        new SpringSettings(15, 1.2f), // Mais macio que metal, mas valores seguros
        staticFriction: 1.2f, 
        dynamicFriction: 1.1f, 
        restitution: 0.1f, 
        density: 1200f, // kg/m³
        maximumRecoveryVelocity: 1f, // Reduzido
        contactDamping: 0.1f); // Reduzido
    
    /// <summary>
    /// Gelo - muito escorregadio
    /// </summary>
    public static Material Ice => new(
        new SpringSettings(25, 0.8f), // Valores seguros
        staticFriction: 0.05f, 
        dynamicFriction: 0.03f, 
        restitution: 0.1f, 
        density: 917f, // kg/m³
        maximumRecoveryVelocity: 1f, // Reduzido
        contactDamping: 0.05f); // Muito reduzido
    
    /// <summary>
    /// Material padrão da engine (valores balanceados)
    /// </summary>
    public static Material Default => new(
        new SpringSettings(30, 1), // Valores padrão do BepuPhysics
        staticFriction: 1f, 
        dynamicFriction: 1f, 
        restitution: 0.2f, 
        density: 1000f, // kg/m³
        maximumRecoveryVelocity: 2f, // Mantido
        contactDamping: 0.0f); // Zero para máxima estabilidade
    
    /// <summary>
    /// Borracha elástica - muito saltitante
    /// </summary>
    public static Material RubberBouncy => new(
        new SpringSettings(20, 0.5f), // Valores conservadores mas permitem elasticidade
        staticFriction: 1.2f, 
        dynamicFriction: 1.1f, 
        restitution: 0.8f, // Reduzido de 0.9f para 0.8f
        density: 1200f, // kg/m³
        maximumRecoveryVelocity: 3f, // Reduzido de 4f para 3f
        contactDamping: 0.02f); // Mínimo para manter estabilidade
    
    /// <summary>
    /// Metal liso - baixo atrito
    /// </summary>
    public static Material MetalSmooth => new(
        new SpringSettings(30, 1), // Padrão BepuPhysics
        staticFriction: 0.1f, 
        dynamicFriction: 0.08f, 
        restitution: 0.3f, 
        density: 7850f, // kg/m³
        maximumRecoveryVelocity: 2f, // Mantido
        contactDamping: 0.0f);
    
    /// <summary>
    /// Madeira áspera - alto atrito
    /// </summary>
    public static Material WoodRough => new(
        new SpringSettings(20, 1.2f), // Conservador
        staticFriction: 0.8f, 
        dynamicFriction: 0.7f, 
        restitution: 0.05f, 
        density: 700f, // kg/m³
        maximumRecoveryVelocity: 1f, // Mantido
        contactDamping: 0.1f); // Reduzido
    
    /// <summary>
    /// Gelo super escorregadio
    /// </summary>
    public static Material IceSlippery => new(
        new SpringSettings(25, 0.8f), // Seguro
        staticFriction: 0.01f, 
        dynamicFriction: 0.005f, 
        restitution: 0.05f, 
        density: 917f, // kg/m³
        maximumRecoveryVelocity: 1f, // Reduzido
        contactDamping: 0.02f); // Mínimo
    
    /// <summary>
    /// Vidro - frágil mas liso
    /// </summary>
    public static Material Glass => new(
        new SpringSettings(35, 0.8f), // Rígido mas conservador
        staticFriction: 0.5f, 
        dynamicFriction: 0.4f, 
        restitution: 0.1f,
        density: 2500f, // kg/m³
        maximumRecoveryVelocity: 2f, // Mantido
        contactDamping: 0.01f); // Mínimo
    
    /// <summary>
    /// Concreto - material pesado e áspero
    /// </summary>
    public static Material Concrete => new(
        new SpringSettings(30, 1), // Valores padrão do BepuPhysics
        staticFriction: 1f, 
        dynamicFriction: 1f, 
        restitution: 0.2f,
        density: 2400f, // kg/m³
        maximumRecoveryVelocity: 2f, // Mantido
        contactDamping: 0.0f); // Zero para máxima estabilidade
}