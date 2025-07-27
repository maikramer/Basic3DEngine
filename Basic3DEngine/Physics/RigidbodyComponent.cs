using System.Numerics;
using Basic3DEngine.Entities;
using BepuPhysics;

namespace Basic3DEngine.Physics;

public class RigidbodyComponent : Component
{
    private readonly PhysicsWorldBepu _physicsWorld;

    public RigidbodyComponent(PhysicsWorldBepu physicsWorld, float mass = 1f, bool isStatic = false)
    {
        _physicsWorld = physicsWorld;
        Mass = mass;
        IsStatic = isStatic;
        Material = Material.Default;
        BodyHandle = new BodyHandle(); // Inicializar com um valor padrão
    }

    public BodyHandle BodyHandle { get; set; }
    public bool IsStatic { get; set; }
    public float Mass { get; set; }
    public Material Material { get; set; }

    public override void Update(float deltaTime)
    {
        // Atualizar a posição do GameObject com base na posição do corpo físico
        if (!IsStatic && _physicsWorld != null)
        {
            // Note: A atualização da posição do GameObject será feita no PhysicsWorldBepu
            // para manter a sincronização com a simulação física
        }
    }

    // Métodos para aplicar forças e impulsos
    public void AddForce(Vector3 force)
    {
        // Não aplicar forças a corpos estáticos
        if (IsStatic || _physicsWorld == null)
            return;

        // Obter a referência ao corpo físico
        var bodyReference = _physicsWorld.GetBodyReference(BodyHandle);

        // Aplicar a força como um impulso linear
        // Nota: Para uma força contínua, precisaríamos acumular e aplicar durante a integração,
        // mas para simplificar, estamos aplicando como um impulso linear proporcional à massa
        bodyReference.ApplyLinearImpulse(force * Mass);
    }

    public void AddTorque(Vector3 torque)
    {
        // Não aplicar torques a corpos estáticos
        if (IsStatic || _physicsWorld == null)
            return;

        // Obter a referência ao corpo físico
        var bodyReference = _physicsWorld.GetBodyReference(BodyHandle);

        // Aplicar o torque como um impulso angular
        bodyReference.ApplyAngularImpulse(torque);
    }

    public void AddImpulse(Vector3 impulse)
    {
        // Não aplicar impulsos a corpos estáticos
        if (IsStatic || _physicsWorld == null)
            return;

        // Obter a referência ao corpo físico
        var bodyReference = _physicsWorld.GetBodyReference(BodyHandle);

        // Aplicar o impulso no centro de massa (offset zero)
        bodyReference.ApplyImpulse(impulse, Vector3.Zero);
    }
}