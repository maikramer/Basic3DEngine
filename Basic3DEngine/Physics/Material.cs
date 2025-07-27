using BepuPhysics.Constraints;

namespace Basic3DEngine.Physics;

public struct Material
{
    public SpringSettings SpringSettings;
    public float FrictionCoefficient;
    public float MaximumRecoveryVelocity;

    public Material(SpringSettings springSettings, float frictionCoefficient = 1f, float maximumRecoveryVelocity = 2f)
    {
        SpringSettings = springSettings;
        FrictionCoefficient = frictionCoefficient;
        MaximumRecoveryVelocity = maximumRecoveryVelocity;
    }

    // Predefined materials for convenience
    public static Material Wood => new(new SpringSettings(15, 0.5f), 0.3f, 1.5f);
    public static Material Metal => new(new SpringSettings(30, 0.1f), 0.2f, 3f);
    public static Material Rubber => new(new SpringSettings(10, 1.5f), 0.8f, 0.5f);
    public static Material Ice => new(new SpringSettings(20, 0.05f), 0.05f, 2.5f);
    public static Material Default => new(new SpringSettings(30, 1));
}