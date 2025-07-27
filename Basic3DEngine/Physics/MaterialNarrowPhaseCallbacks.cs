using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;

namespace Basic3DEngine.Physics;

public struct MaterialNarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    /// <summary>
    ///     Maps <see cref="CollidableReference" /> entries to their <see cref="Material" />.
    /// </summary>
    public CollidableProperty<Material> CollidableMaterials;

    public void Initialize(Simulation simulation)
    {
        //The callbacks get created before the simulation so that they can be given to the simulation. The property needs a simulation reference, so we hand it over in the initialize.
        CollidableMaterials.Initialize(simulation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
        ref float speculativeMargin)
    {
        //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
        //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
        //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
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
        //For the purposes of this demo, we'll use multiplicative blending for the friction and choose spring properties according to which collidable has a higher maximum recovery velocity.
        var a = CollidableMaterials[pair.A];
        var b = CollidableMaterials[pair.B];
        pairMaterial.FrictionCoefficient = a.FrictionCoefficient * b.FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MathF.Max(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity);
        pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == a.MaximumRecoveryVelocity
            ? a.SpringSettings
            : b.SpringSettings;
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