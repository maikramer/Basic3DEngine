using System.Numerics;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Physics.Shapes;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Basic3DEngine.Physics;

/// <summary>
/// Modo de física da simulação
/// </summary>
public enum PhysicsMode
{
    /// <summary>
    /// Modo simples com callbacks básicos (padrão)
    /// </summary>
    Simple,
    
    /// <summary>
    /// Modo avançado com propriedades por corpo
    /// </summary>
    Advanced
}

/// <summary>
/// Mundo físico usando BepuPhysics v2
/// </summary>
public sealed class PhysicsWorldBepu : IDisposable
{
    private BufferPool _bufferPool;
    private CollidableProperty<Material> _collidableMaterials;
    private CollidableProperty<BodyProperties>? _bodyPropertiesData;
    private readonly List<RigidbodyComponent> _rigidbodies = new();
    private Simulation _simulation;
    private ThreadDispatcher _threadDispatcher;
    private PhysicsMode _currentMode = PhysicsMode.Simple;
    private AdvancedPoseIntegratorCallbacks? _advancedCallbacks;
    
    // Configurações padrão
    private Vector3 _defaultGravity = new(0, -9.81f, 0);
    private float _defaultLinearDamping = 0.03f;
    private float _defaultAngularDamping = 0.03f;

    public PhysicsWorldBepu()
    {
        // Inicializar o pool de buffers
        _bufferPool = new BufferPool();

        // Inicializar as propriedades de materiais (sem simulação ainda)
        _collidableMaterials = new CollidableProperty<Material>();

        // Criar a simulação no modo simples por padrão
        CreateSimulation(PhysicsMode.Simple);

        // Criar o dispatcher de threads
        _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
    }
    
    /// <summary>
    /// Modo de física atual
    /// </summary>
    public PhysicsMode Mode => _currentMode;
    
    /// <summary>
    /// Define o modo de física (simples ou avançado)
    /// </summary>
    public void SetPhysicsMode(PhysicsMode mode)
    {
        if (_currentMode == mode) return;
        
        // Guardar referência aos rigidbodies existentes
        var oldRigidbodies = _rigidbodies.ToList();
        
        // Limpar simulação atual
        _simulation?.Dispose();
        
        // Recriar _collidableMaterials para a nova simulação
        _collidableMaterials = new CollidableProperty<Material>();
        
        // Criar nova simulação com o modo desejado
        CreateSimulation(mode);
        _currentMode = mode;
        
        // Re-adicionar todos os corpos
        foreach (var rb in oldRigidbodies)
        {
            if (rb.Shape != null)
            {
                AddBody(rb);
            }
        }
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
    
    private void CreateSimulation(PhysicsMode mode)
    {
        // Criar callbacks de narrow phase
        var narrowPhaseCallbacks = new MaterialNarrowPhaseCallbacks(0.01f, 1.0f);
        
        if (mode == PhysicsMode.Simple)
        {
            // Modo simples - usar callbacks básicos
            _simulation = Simulation.Create(
                _bufferPool,
                narrowPhaseCallbacks,
                new PoseIntegratorCallbacks(_defaultGravity, _defaultLinearDamping, _defaultAngularDamping),
                new SolveDescription(8, 1)
            );
            
            // Inicializar o CollidableMaterials com a simulação
            _collidableMaterials.Initialize(_simulation);
            
            // Configurar o CollidableMaterials nos callbacks
            narrowPhaseCallbacks.CollidableMaterials = _collidableMaterials;
            
            _bodyPropertiesData = null;
            _advancedCallbacks = null;
        }
        else
        {
            // Modo avançado - usar callbacks com propriedades por corpo
            _advancedCallbacks = new AdvancedPoseIntegratorCallbacks(
                _defaultGravity, 
                _defaultLinearDamping, 
                _defaultAngularDamping,
                AngularIntegrationMode.ConserveMomentum
            );
            
            _simulation = Simulation.Create(
                _bufferPool,
                narrowPhaseCallbacks,
                _advancedCallbacks.Value,
                new SolveDescription(8, 1)
            );
            
            // Inicializar o CollidableMaterials com a simulação
            _collidableMaterials.Initialize(_simulation);
            
            // Configurar o CollidableMaterials nos callbacks
            narrowPhaseCallbacks.CollidableMaterials = _collidableMaterials;
            
            // Inicializar propriedades por corpo se ainda não existir
            if (_bodyPropertiesData == null)
            {
                _bodyPropertiesData = new CollidableProperty<BodyProperties>();
                _bodyPropertiesData.Initialize(_simulation);
            }
        }
    }

    /// <summary>
    /// Adiciona um corpo à simulação
    /// </summary>
    public void AddBody(RigidbodyComponent rigidbody)
    {
        if (rigidbody.Shape == null)
            throw new InvalidOperationException("Rigidbody must have a shape before being added to physics world");

        var shapeIndex = rigidbody.Shape.CreateShape(_simulation.Shapes);
        
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
                0.01f // Atividade
            );

            var handle = _simulation.Bodies.Add(bodyDescription);
            rigidbody.BodyHandle = handle;

            // Associar material ao corpo dinâmico
            _collidableMaterials.Allocate(handle) = rigidbody.Material;
            
            // Se estiver em modo avançado e o rigidbody tem BodyProperties, registrar
            if (_currentMode == PhysicsMode.Advanced && rigidbody.BodyProperties.HasValue && _advancedCallbacks.HasValue)
            {
                RegisterBodyProperties(handle, rigidbody.BodyProperties.Value);
            }

            // Adicionar à lista de rigidbodies dinâmicos
            _rigidbodies.Add(rigidbody);
        }
    }
    
    /// <summary>
    /// Registra propriedades customizadas para um corpo
    /// </summary>
    public void RegisterBodyProperties(BodyHandle handle, BodyProperties properties)
    {
        if (_currentMode == PhysicsMode.Advanced && _advancedCallbacks.HasValue)
        {
            _advancedCallbacks.Value.RegisterBodyProperties(handle.Value, properties);
        }
    }
    
    /// <summary>
    /// Remove propriedades customizadas de um corpo
    /// </summary>
    public void UnregisterBodyProperties(BodyHandle handle)
    {
        if (_currentMode == PhysicsMode.Advanced && _advancedCallbacks.HasValue)
        {
            _advancedCallbacks.Value.UnregisterBodyProperties(handle.Value);
        }
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
                // Remover propriedades customizadas se existirem
                if (_currentMode == PhysicsMode.Advanced)
                {
                    UnregisterBodyProperties(rigidbody.BodyHandle);
                }
                
                _simulation.Bodies.Remove(rigidbody.BodyHandle);
                _rigidbodies.Remove(rigidbody);
            }
        }
    }

    /// <summary>
    /// Adiciona um corpo com forma de caixa (método de conveniência mantido para compatibilidade)
    /// </summary>
    public void AddBox(RigidbodyComponent rigidbody, Vector3 size)
    {
        rigidbody.Shape = new BoxShape(size);
        AddBody(rigidbody);
    }

    /// <summary>
    /// Adiciona um corpo com forma de esfera (método de conveniência mantido para compatibilidade)
    /// </summary>
    public void AddSphere(RigidbodyComponent rigidbody, float radius)
    {
        rigidbody.Shape = new SphereShape(radius);
        AddBody(rigidbody);
    }

    /// <summary>
    /// Atualiza a simulação
    /// </summary>
    public void Update(float deltaTime)
    {
        // Atualizar a simulação
        _simulation.Timestep(deltaTime, _threadDispatcher);
    }

    /// <summary>
    /// Obtém uma referência ao corpo físico
    /// </summary>
    public BodyReference GetBodyReference(BodyHandle handle)
    {
        return _simulation.Bodies[handle];
    }

    /// <summary>
    /// Faz um raycast no mundo físico
    /// </summary>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        hit = default;
        
        var ray = new RayData
        {
            Origin = origin,
            Direction = Vector3.Normalize(direction) * maxDistance,
            Id = 0
        };

        var handler = new RayHitHandler();
        _simulation.RayCast(ray.Origin, ray.Direction, maxDistance, ref handler);

        if (handler.Hit)
        {
            hit = new RaycastHit
            {
                Point = handler.HitLocation,
                Normal = handler.HitNormal,
                Distance = handler.T * maxDistance,
                CollidableReference = handler.CollidableReference
            };
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        _simulation?.Dispose();
        _threadDispatcher?.Dispose();
        _bufferPool?.Clear();
    }
    
    private struct RayHitHandler : IRayHitHandler
    {
        public bool Hit;
        public float T;
        public Vector3 HitLocation;
        public Vector3 HitNormal;
        public CollidableReference CollidableReference;

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
                T = t;
                HitLocation = ray.Origin + ray.Direction * t;
                HitNormal = normal;
                CollidableReference = collidable;
                maximumT = t;
            }
        }
    }
}

/// <summary>
/// Resultado de um raycast
/// </summary>
public struct RaycastHit
{
    public Vector3 Point;
    public Vector3 Normal;
    public float Distance;
    public CollidableReference CollidableReference;
}