using System;
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
        
        // CORREÇÃO: Aplicar restituição de forma mais conservadora e estável
        // A fórmula anterior criava velocidades extremas que causavam NaN
        // Nova fórmula: usar restitution como multiplicador direto, limitado a valores seguros
        var safeRestitution = Math.Clamp(restitution, 0f, 0.95f); // Limitar restitution
        var safeMaxRecoveryVelocity = Math.Clamp(maxRecoveryVelocity, 0.1f, 10f); // Limitar velocidade base
        
        // Aplicar restituição de forma linear e controlada
        pairMaterial.MaximumRecoveryVelocity = safeMaxRecoveryVelocity * (0.5f + safeRestitution * 0.5f);
        
        // Garantir que nunca exceda um limite absoluto seguro
        pairMaterial.MaximumRecoveryVelocity = Math.Min(pairMaterial.MaximumRecoveryVelocity, 8f);
        
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
        
        // CORREÇÃO: Usar SpringSettings mais simples e estáveis
        // Baseado nas demos oficiais do BepuPhysics que usam SpringSettings(30, 1)
        SpringSettings springA = materialA.SpringSettings;
        SpringSettings springB = materialB.SpringSettings;
        
        // Verificar se as SpringSettings são válidas primeiro
        if (!SpringSettings.Validate(springA))
        {
            springA = new SpringSettings(30, 1); // Fallback seguro
        }
        if (!SpringSettings.Validate(springB))
        {
            springB = new SpringSettings(30, 1); // Fallback seguro
        }
        
        // Usar a frequency menor para evitar instabilidade
        // (frequencies muito altas podem causar problemas numéricos)
        float safeFrequency = Math.Min(springA.Frequency, springB.Frequency);
        float safeDampingRatio = Math.Max(springA.DampingRatio, springB.DampingRatio);
        
        // Garantir valores seguros (baseado nas demos do BepuPhysics)
        safeFrequency = Math.Clamp(safeFrequency, 5f, 50f);  // Entre 5 e 50 Hz
        safeDampingRatio = Math.Clamp(safeDampingRatio, 0.1f, 2f);  // Entre 0.1 e 2.0
        
        pairMaterial.SpringSettings = new SpringSettings(safeFrequency, safeDampingRatio);
        
        // REMOVIDO: Lógica complexa de contact damping que estava causando instabilidade
        // A modificação dinâmica das SpringSettings pode causar valores inválidos
        
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