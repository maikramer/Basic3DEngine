using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Basic3DEngine.Physics;

public class PhysicsWorldBepu : IDisposable
{
    private readonly BufferPool _bufferPool;
    private readonly CollidableProperty<Material> _collidableMaterials;
    private readonly List<PhysicsEntityBepu> _physicsEntities;
    private readonly Simulation _simulation;
    private readonly ThreadDispatcher _threadDispatcher;

    public PhysicsWorldBepu()
    {
        // Inicializar o pool de buffers
        _bufferPool = new BufferPool();

        // Inicializar as propriedades de materiais
        _collidableMaterials = new CollidableProperty<Material>();

        // Criar a simulação com callbacks padrão
        _simulation = Simulation.Create(
            _bufferPool,
            new MaterialNarrowPhaseCallbacks { CollidableMaterials = _collidableMaterials },
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)),
            new SolveDescription(8, 1)
        );

        // Criar o dispatcher de threads
        _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

        // Inicializar a lista de entidades físicas
        _physicsEntities = new List<PhysicsEntityBepu>();
    }

    public void Dispose()
    {
        _simulation?.Dispose();
        _threadDispatcher?.Dispose();
        _bufferPool?.Clear();
    }

    public void AddBox(RigidbodyComponent rigidbody, Vector3 size)
    {
        // Criar uma caixa no BepuPhysics
        var box = new Box(size.X, size.Y, size.Z);

        if (rigidbody.IsStatic)
        {
            // Adicionar como corpo estático
            var staticDescription = new StaticDescription(rigidbody.GameObject.Position, _simulation.Shapes.Add(box));
            var staticHandle = _simulation.Statics.Add(staticDescription);

            // Associar material ao corpo estático
            _collidableMaterials.Allocate(staticHandle) = rigidbody.Material;
        }
        else
        {
            // Calcular inércia para corpo dinâmico
            var inertia = box.ComputeInertia(rigidbody.Mass);

            // Adicionar como corpo dinâmico
            var bodyDescription = BodyDescription.CreateDynamic(
                new RigidPose(rigidbody.GameObject.Position, Quaternion.Identity),
                inertia,
                _simulation.Shapes.Add(box),
                0.01f // Atividade
            );

            var handle = _simulation.Bodies.Add(bodyDescription);
            rigidbody.BodyHandle = handle;

            // Associar material ao corpo dinâmico
            _collidableMaterials.Allocate(handle) = rigidbody.Material;

            // Adicionar à lista de entidades físicas para atualização
            _physicsEntities.Add(new PhysicsEntityBepu(rigidbody.GameObject, handle));
        }
    }

    public void AddSphere(RigidbodyComponent rigidbody, float radius)
    {
        // Criar uma esfera no BepuPhysics
        var sphere = new Sphere(radius);

        if (rigidbody.IsStatic)
        {
            // Adicionar como corpo estático
            var staticDescription =
                new StaticDescription(rigidbody.GameObject.Position, _simulation.Shapes.Add(sphere));
            var staticHandle = _simulation.Statics.Add(staticDescription);

            // Associar material ao corpo estático
            _collidableMaterials.Allocate(staticHandle) = rigidbody.Material;
        }
        else
        {
            // Calcular inércia para corpo dinâmico
            var inertia = sphere.ComputeInertia(rigidbody.Mass);

            // Adicionar como corpo dinâmico
            var bodyDescription = BodyDescription.CreateDynamic(
                new RigidPose(rigidbody.GameObject.Position, Quaternion.Identity),
                inertia,
                _simulation.Shapes.Add(sphere),
                0.01f // Atividade
            );

            var handle = _simulation.Bodies.Add(bodyDescription);
            rigidbody.BodyHandle = handle;

            // Associar material ao corpo dinâmico
            _collidableMaterials.Allocate(handle) = rigidbody.Material;

            // Adicionar à lista de entidades físicas para atualização
            _physicsEntities.Add(new PhysicsEntityBepu(rigidbody.GameObject, handle));
        }
    }

    public void Update(float deltaTime)
    {
        // Atualizar a simulação
        _simulation.Timestep(deltaTime, _threadDispatcher);

        // Atualizar as posições das entidades de renderização
        foreach (var physicsEntity in _physicsEntities) physicsEntity.UpdatePosition(_simulation);
    }

    // Método para obter uma referência ao corpo físico
    public BodyReference GetBodyReference(BodyHandle handle)
    {
        return _simulation.Bodies[handle];
    }
}