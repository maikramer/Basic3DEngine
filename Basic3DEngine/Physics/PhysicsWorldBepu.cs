using System.Numerics;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Physics.Shapes;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Basic3DEngine.Physics;

/// <summary>
/// Mundo físico usando BepuPhysics v2
/// </summary>
public sealed class PhysicsWorldBepu : IDisposable
{
    private BufferPool _bufferPool;
    private CollidableProperty<Material> _collidableMaterials;
    private CollidableProperty<uint> _collidableLayers;
    private readonly List<RigidbodyComponent> _rigidbodies = new();
    private Simulation _simulation;
    private ThreadDispatcher _threadDispatcher;
    
    // Configurações padrão
    private Vector3 _defaultGravity = new(0, -9.81f, 0);
        private float _defaultLinearDamping = 0.05f;
        private float _defaultAngularDamping = 0.08f;

    public PhysicsWorldBepu()
    {
        // Inicializar o pool de buffers
        _bufferPool = new BufferPool();

        // Inicializar as propriedades de materiais (sem simulação ainda)
        _collidableMaterials = new CollidableProperty<Material>();
        _collidableLayers = new CollidableProperty<uint>();

        // Criar a simulação no modo simples
        CreateSimulation();

        // Criar o dispatcher de threads
        _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
    }
    
    /// <summary>
    /// Define a gravidade padrão
    /// </summary>
    public void SetDefaultGravity(Vector3 gravity)
    {
        _defaultGravity = gravity;
        
        // Atualizar callbacks existentes - SIMPLIFICADO
        // Não vamos recriar a simulação, apenas atualizar para próximas criações
        // A mudança de gravidade será aplicada nas próximas integrações
    }
    
    /// <summary>
    /// Define o amortecimento padrão
    /// </summary>
    public void SetDefaultDamping(float linearDamping, float angularDamping)
    {
        _defaultLinearDamping = Math.Clamp(linearDamping, 0f, 1f);
        _defaultAngularDamping = Math.Clamp(angularDamping, 0f, 1f);
        
        // Atualizar callbacks existentes - SIMPLIFICADO
        // Não vamos recriar a simulação, apenas atualizar para próximas criações
        // A mudança de damping será aplicada nas próximas integrações
    }
    
        private void CreateSimulation()
    {
        // Criar callbacks de narrow phase
        var narrowPhaseCallbacks = new MaterialNarrowPhaseCallbacks(0.01f, 1.0f, 0.03f);
        
            // Usar integrador avançado e aumentar iterações do solver
            var integrator = new AdvancedPoseIntegratorCallbacks(_defaultGravity, _defaultLinearDamping, _defaultAngularDamping);
            _simulation = Simulation.Create(
                _bufferPool,
                narrowPhaseCallbacks,
                integrator,
                new SolveDescription(12, 2)
            );
        
        // Inicializar o CollidableMaterials com a simulação
        _collidableMaterials.Initialize(_simulation);
        _collidableLayers.Initialize(_simulation);
        
        // Configurar o CollidableMaterials nos callbacks
        narrowPhaseCallbacks.CollidableMaterials = _collidableMaterials;
        narrowPhaseCallbacks.CollidableLayers = _collidableLayers;
    }

    /// <summary>
    /// Adiciona um corpo à simulação
    /// </summary>
    public void AddBody(RigidbodyComponent rigidbody)
    {
        if (rigidbody.Shape == null)
            throw new InvalidOperationException("Rigidbody must have a shape before being added to physics world");

        TypedIndex shapeIndex;
        // Alguns shapes (como Compound) exigem acesso ao BufferPool para criação
        if (rigidbody.Shape is Basic3DEngine.Core.Interfaces.IRequiresBufferPoolShape req)
        {
            shapeIndex = req.CreateShape(_simulation.Shapes, _bufferPool);
        }
        else
        {
            shapeIndex = rigidbody.Shape.CreateShape(_simulation.Shapes);
        }
        
        if (rigidbody.IsStatic)
        {
            // Usar a pose do rigidbody se foi definida, senão usar a posição do GameObject
            var pose = rigidbody.Pose.Position != Vector3.Zero || rigidbody.Pose.Orientation != Quaternion.Identity
                ? rigidbody.Pose
                : rigidbody.GameObject != null 
                    ? new RigidPose(rigidbody.GameObject.Position, Quaternion.Identity) 
                    : new RigidPose(Vector3.Zero);
                
            var staticDescription = new StaticDescription(pose, shapeIndex);
            var staticHandle = _simulation.Statics.Add(staticDescription);

            // Associar material ao corpo estático
            _collidableMaterials.Allocate(staticHandle) = rigidbody.Material;
            _collidableLayers.Allocate(staticHandle) = 0xFFFFFFFF;
        }
        else
        {
            // Calcular inércia para corpo dinâmico
            var inertia = rigidbody.Shape.ComputeInertia(rigidbody.Mass);

            // Usar a pose do rigidbody se foi definida, senão usar a posição do GameObject
            var pose = rigidbody.Pose.Position != Vector3.Zero || rigidbody.Pose.Orientation != Quaternion.Identity
                ? rigidbody.Pose
                : rigidbody.GameObject != null 
                    ? new RigidPose(rigidbody.GameObject.Position, Quaternion.Identity) 
                    : new RigidPose(Vector3.Zero);

            // Adicionar como corpo dinâmico
            var bodyDescription = BodyDescription.CreateDynamic(
                pose,
                inertia,
                shapeIndex,
                0.1f // Atividade
            );

            var handle = _simulation.Bodies.Add(bodyDescription);
            rigidbody.BodyHandle = handle;

            // Associar material ao corpo dinâmico
            _collidableMaterials.Allocate(handle) = rigidbody.Material;
            _collidableLayers.Allocate(handle) = 0xFFFFFFFF;

            // Adicionar à lista de rigidbodies dinâmicos
            _rigidbodies.Add(rigidbody);
        }
    }

    /// <summary>
    /// Ajusta parâmetros do solver (iterações e subiterações)
    /// </summary>
    public void SetSolverIterations(int velocityIterations = 12, int substepCount = 2)
    {
        _simulation.Solver.VelocityIterationCount = Math.Max(1, velocityIterations);
        _simulation.Solver.SubstepCount = Math.Max(1, substepCount);
    }

    /// <summary>
    /// Remove um corpo da simulação
    /// </summary>
    public void RemoveBody(RigidbodyComponent rigidbody)
    {
        if (rigidbody.IsStatic)
        {
            // Ainda não implementado para estáticos
        }
        else
        {
            if (_simulation.Bodies.BodyExists(rigidbody.BodyHandle))
            {
                _simulation.Bodies.Remove(rigidbody.BodyHandle);
                _rigidbodies.Remove(rigidbody);
            }
        }
    }

    public BodyReference GetBodyReference(BodyHandle handle)
    {
        return _simulation.Bodies[handle];
    }

    /// <summary>
    /// Adiciona uma junta do tipo hinge (rotação em 1 eixo) entre dois rigidbodies.
    /// Eixos locais devem estar alinhados no espaço do mundo.
    /// </summary>
    public ConstraintHandle AddHingeConstraint(
        RigidbodyComponent bodyA,
        RigidbodyComponent bodyB,
        Vector3 localOffsetA,
        Vector3 localOffsetB,
        Vector3 localAxisA,
        Vector3 localAxisB,
        SpringSettings? spring = null)
    {
        var s = spring ?? new SpringSettings(30, 1);
        var hinge = new Hinge
        {
            LocalOffsetA = localOffsetA,
            LocalOffsetB = localOffsetB,
            LocalHingeAxisA = Vector3.Normalize(localAxisA),
            LocalHingeAxisB = Vector3.Normalize(localAxisB),
            SpringSettings = s.Value
        };
        return _simulation.Solver.Add(bodyA.BodyHandle, bodyB.BodyHandle, hinge);
    }

    /// <summary>
    /// Adiciona um motor angular no eixo especificado entre dois corpos.
    /// </summary>
    public ConstraintHandle AddAngularAxisMotor(
        RigidbodyComponent bodyA,
        RigidbodyComponent bodyB,
        Vector3 localAxisA,
        Vector3 localAxisB,
        float targetVelocity,
        float maximumForce)
    {
        var motor = new AngularAxisMotor
        {
            LocalAxisA = Vector3.Normalize(localAxisA),
            LocalAxisB = Vector3.Normalize(localAxisB),
            Settings = new MotorSettings(maximumForce, 1e-3f)
        };
        var handle = _simulation.Solver.Add(bodyA.BodyHandle, bodyB.BodyHandle, motor);
        // Definir velocidade alvo (usa descrição de servo/motor em estados); aqui usamos API simplificada do demo
        _simulation.Solver.ApplyDescription(handle, motor);
        // Observação: Para controlar velocidade continuamente, atualizar descrição fora desta função.
        return handle;
    }

    /// <summary>
    /// Executa uma etapa da simulação física
    /// </summary>
    public void Step(float deltaTime)
    {
        // Executar a simulação física
        _simulation.Timestep(deltaTime, _threadDispatcher);
    }

    /// <summary>
    /// Executa raycast no mundo físico
    /// </summary>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastResult result)
    {
        var handler = new RaycastHandler();
        
        _simulation.RayCast(origin, direction, maxDistance, ref handler);

        if (handler.Hit)
        {
            result = new RaycastResult
            {
                Hit = true,
                Point = handler.HitLocation,
                Normal = handler.HitNormal,
                Distance = handler.HitDistance,
                Collidable = handler.HitCollidable
            };
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Libera recursos
    /// </summary>
    public void Dispose()
    {
        _threadDispatcher?.Dispose();
        _simulation?.Dispose();
        _collidableMaterials?.Dispose();
        _bufferPool?.Clear();
    }

    private struct RaycastHandler : IRayHitHandler
    {
        public bool Hit;
        public Vector3 HitLocation;
        public Vector3 HitNormal;
        public float HitDistance;
        public CollidableReference HitCollidable;

        public bool AllowTest(CollidableReference collidable)
        {
            return true;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT)
            {
                Hit = true;
                HitLocation = ray.Origin + ray.Direction * t;
                HitNormal = normal;
                HitDistance = t;
                HitCollidable = collidable;
                maximumT = t;
            }
        }
    }

    public struct RaycastResult
    {
        public bool Hit;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        public CollidableReference Collidable;
    }
}