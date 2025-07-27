using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace Basic3DEngine.Physics;

/// <summary>
/// Callbacks para fase estreita de colisão com suporte a materiais físicos avançados
/// </summary>
public struct MaterialNarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    /// <summary>
    /// Mapeia referências de colisão para seus materiais
    /// </summary>
    public CollidableProperty<Material> CollidableMaterials;
    
    /// <summary>
    /// Threshold para aplicação de atrito estático
    /// </summary>
    public float StaticFrictionThreshold;
    
    /// <summary>
    /// Escala para atrito rotacional (twist friction)
    /// </summary>
    public float TwistFrictionScale;
    
    /// <summary>
    /// Cria callbacks com valores padrão
    /// </summary>
    public MaterialNarrowPhaseCallbacks(float staticFrictionThreshold = 0.01f, float twistFrictionScale = 1.0f) : this()
    {
        StaticFrictionThreshold = staticFrictionThreshold;
        TwistFrictionScale = twistFrictionScale;
        CollidableMaterials = new CollidableProperty<Material>();
    }

    public void Initialize(Simulation simulation)
    {
        CollidableMaterials.Initialize(simulation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
        ref float speculativeMargin)
    {
        // Pelo menos um dos corpos precisa ser dinâmico
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Obter materiais dos corpos em colisão
        var materialA = CollidableMaterials[pair.A];
        var materialB = CollidableMaterials[pair.B];
        
        // Calcular propriedades interpoladas entre os materiais
        // Para atrito, usamos a média geométrica (multiplicação) que é fisicamente mais correta
        var staticFriction = MathF.Sqrt(materialA.StaticFriction * materialB.StaticFriction);
        var dynamicFriction = MathF.Sqrt(materialA.DynamicFriction * materialB.DynamicFriction);
        
        // Para restituição, usamos o mínimo (modelo mais conservador)
        var restitution = MathF.Min(materialA.Restitution, materialB.Restitution);
        
        // Para velocidade máxima de recuperação, usamos o máximo
        var maxRecoveryVelocity = MathF.Max(materialA.MaximumRecoveryVelocity, materialB.MaximumRecoveryVelocity);
        
        // Aplicar restituição à velocidade de recuperação
        // Quanto maior a restituição, maior a velocidade permitida para "quicar"
        pairMaterial.MaximumRecoveryVelocity = maxRecoveryVelocity * (1.0f + restitution * 2.0f);
        
        // Calcular velocidade relativa aproximada para determinar se usar atrito estático ou dinâmico
        // Nota: Esta é uma aproximação. Para velocidade exata, precisaríamos acessar os corpos
        float frictionCoefficient;
        
        // Se ambos os corpos são estáticos ou muito lentos, usar atrito estático
        if (pair.A.Mobility != CollidableMobility.Dynamic && pair.B.Mobility != CollidableMobility.Dynamic)
        {
            frictionCoefficient = staticFriction;
        }
        else
        {
            // Por padrão, usar uma mistura ponderada entre estático e dinâmico
            // Isso simula a transição gradual entre os dois regimes
            float staticWeight = 0.3f; // 30% estático, 70% dinâmico por padrão
            frictionCoefficient = staticFriction * staticWeight + dynamicFriction * (1f - staticWeight);
        }
        
        pairMaterial.FrictionCoefficient = frictionCoefficient;
        
        // Configurar SpringSettings combinando os dois materiais
        // Usar o material mais rígido (maior frequência) como base
        SpringSettings springA = materialA.SpringSettings;
        SpringSettings springB = materialB.SpringSettings;
        
        if (springA.Frequency > springB.Frequency)
        {
            pairMaterial.SpringSettings = springA;
        }
        else if (springB.Frequency > springA.Frequency)
        {
            pairMaterial.SpringSettings = springB;
        }
        else
        {
            // Se frequências são iguais, usar média do damping ratio
            var avgDampingRatio = (springA.DampingRatio + springB.DampingRatio) * 0.5f;
            pairMaterial.SpringSettings = new SpringSettings(springA.Frequency, avgDampingRatio);
        }
        
        // Aplicar contact damping combinado
        var contactDamping = (materialA.ContactDamping + materialB.ContactDamping) * 0.5f;
        if (contactDamping > 0)
        {
            // Reduzir a frequência da mola baseado no damping para simular absorção de impacto
            pairMaterial.SpringSettings.Frequency *= (1f - contactDamping * 0.5f);
            pairMaterial.SpringSettings.DampingRatio += contactDamping;
        }
        
        // Para suportar TwistFriction (rotação durante colisões), o coeficiente de atrito
        // é automaticamente usado pelo BepuPhysics para calcular o torque máximo permitido
        // Podemos escalar isso baseado na área de contato estimada
        if (TwistFrictionScale != 1.0f)
        {
            // O twist friction é proporcional ao atrito normal
            // mas pode ser escalado para efeitos específicos
            pairMaterial.FrictionCoefficient *= TwistFrictionScale;
        }
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }
}