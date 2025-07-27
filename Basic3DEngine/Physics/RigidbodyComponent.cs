using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Entities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;

namespace Basic3DEngine.Physics;

/// <summary>
/// Componente que adiciona física a um GameObject
/// </summary>
public sealed class RigidbodyComponent : Component
{
    private readonly PhysicsWorldBepu _physicsWorld;
    private readonly float _mass;
    private readonly bool _isStatic;
    
    // Estado físico
    private RigidPose _pose;
    private Vector3 _linearVelocity;
    private Vector3 _angularVelocity;
    private BodyProperties? _bodyProperties;
    private Vector3 _centerOfMass = Vector3.Zero;
    
    /// <summary>
    /// Referência ao corpo no mundo físico
    /// </summary>
    public BodyHandle BodyHandle { get; set; } = new BodyHandle(-1);
    
    /// <summary>
    /// Forma física do corpo
    /// </summary>
    public IPhysicsShape? Shape { get; set; }
    
    /// <summary>
    /// Material físico do corpo
    /// </summary>
    public Material Material { get; set; } = Material.Default;
    
    /// <summary>
    /// Pose (posição e orientação) do corpo
    /// </summary>
    public RigidPose Pose
    {
        get => _pose;
        set
        {
            _pose = value;
            if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
            {
                var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
                bodyRef.Pose = value;
            }
        }
    }
    
    /// <summary>
    /// Velocidade linear do corpo
    /// </summary>
    public Vector3 LinearVelocity
    {
        get => _linearVelocity;
        set
        {
            _linearVelocity = value;
            if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
            {
                var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
                bodyRef.Velocity.Linear = value;
            }
        }
    }
    
    /// <summary>
    /// Velocidade angular do corpo
    /// </summary>
    public Vector3 AngularVelocity
    {
        get => _angularVelocity;
        set
        {
            _angularVelocity = value;
            if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
            {
                var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
                bodyRef.Velocity.Angular = value;
            }
        }
    }
    
    /// <summary>
    /// Propriedades físicas avançadas do corpo
    /// </summary>
    public BodyProperties? BodyProperties
    {
        get => _bodyProperties;
        set
        {
            _bodyProperties = value;
            if (_physicsWorld != null && BodyHandle.Value >= 0)
            {
                OnBodyPropertiesChanged();
            }
        }
    }
    
    /// <summary>
    /// Centro de massa do corpo
    /// </summary>
    public Vector3 CenterOfMass 
    { 
        get => _centerOfMass;
        set => _centerOfMass = value;
    }
    
    /// <summary>
    /// Indica se o corpo é estático
    /// </summary>
    public bool IsStatic => _isStatic;
    
    /// <summary>
    /// Massa do corpo
    /// </summary>
    public float Mass => _mass;
    
    public RigidbodyComponent(PhysicsWorldBepu physicsWorld, float mass = 1f, bool isStatic = false)
    {
        _physicsWorld = physicsWorld;
        _mass = mass;
        _isStatic = isStatic;
        _pose = new RigidPose(Vector3.Zero, Quaternion.Identity);
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        if (!IsStatic && _physicsWorld != null && GameObject != null)
        {
            // Obter estado atual do corpo físico
            var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
            _pose = bodyRef.Pose;
            _linearVelocity = bodyRef.Velocity.Linear;
            _angularVelocity = bodyRef.Velocity.Angular;
            
            // Aplicar limites de velocidade se configurados
            if (_bodyProperties.HasValue)
            {
                bool velocityChanged = false;
                
                // Limitar velocidade linear
                if (_bodyProperties.Value.MaxLinearVelocity < float.MaxValue)
                {
                    var speed = _linearVelocity.Length();
                    if (speed > _bodyProperties.Value.MaxLinearVelocity)
                    {
                        _linearVelocity = _linearVelocity * (_bodyProperties.Value.MaxLinearVelocity / speed);
                        bodyRef.Velocity.Linear = _linearVelocity;
                        velocityChanged = true;
                    }
                }
                
                // Limitar velocidade angular
                if (_bodyProperties.Value.MaxAngularVelocity < float.MaxValue)
                {
                    var angularSpeed = _angularVelocity.Length();
                    if (angularSpeed > _bodyProperties.Value.MaxAngularVelocity)
                    {
                        _angularVelocity = _angularVelocity * (_bodyProperties.Value.MaxAngularVelocity / angularSpeed);
                        bodyRef.Velocity.Angular = _angularVelocity;
                        velocityChanged = true;
                    }
                }
                
                if (velocityChanged)
                {
                    bodyRef.Velocity = new BodyVelocity { Linear = _linearVelocity, Angular = _angularVelocity };
                }
            }
            
            // Atualizar posição e rotação do GameObject
            GameObject.Position = _pose.Position;
            GameObject.Rotation = QuaternionToEuler(_pose.Orientation);
        }
    }
    
    /// <summary>
    /// Aplica uma força ao corpo
    /// </summary>
    public void AddForce(Vector3 force)
    {
        if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
        {
            var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
            bodyRef.ApplyLinearImpulse(force * Time.FixedDeltaTime);
        }
    }
    
    /// <summary>
    /// Aplica um torque ao corpo
    /// </summary>
    public void AddTorque(Vector3 torque)
    {
        if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
        {
            var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
            bodyRef.ApplyAngularImpulse(torque * Time.FixedDeltaTime);
        }
    }
    
    /// <summary>
    /// Aplica um impulso ao corpo
    /// </summary>
    public void AddImpulse(Vector3 impulse)
    {
        if (!IsStatic && _physicsWorld != null && BodyHandle.Value >= 0)
        {
            var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
    }
    
    /// <summary>
    /// Define as velocidades linear e angular do corpo
    /// </summary>
    public void SetVelocity(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        LinearVelocity = linearVelocity;
        AngularVelocity = angularVelocity;
    }
    
    /// <summary>
    /// Para o corpo completamente
    /// </summary>
    public void Stop()
    {
        SetVelocity(Vector3.Zero, Vector3.Zero);
    }
    
    /// <summary>
    /// Define o amortecimento linear
    /// </summary>
    public void SetLinearDamping(float damping)
    {
        var props = _bodyProperties ?? Physics.BodyProperties.Default;
        _bodyProperties = props with { LinearDamping = Math.Clamp(damping, 0f, 1f) };
        OnBodyPropertiesChanged();
    }
    
    /// <summary>
    /// Define o amortecimento angular
    /// </summary>
    public void SetAngularDamping(float damping)
    {
        var props = _bodyProperties ?? Physics.BodyProperties.Default;
        _bodyProperties = props with { AngularDamping = Math.Clamp(damping, 0f, 1f) };
        OnBodyPropertiesChanged();
    }
    
    /// <summary>
    /// Define a escala de gravidade
    /// </summary>
    public void SetGravityScale(float scale)
    {
        var props = _bodyProperties ?? Physics.BodyProperties.Default;
        _bodyProperties = props with { GravityScale = Math.Max(0f, scale) };
        OnBodyPropertiesChanged();
    }
    
    /// <summary>
    /// Define os limites de velocidade
    /// </summary>
    public void SetVelocityLimits(float maxLinearVelocity, float maxAngularVelocity)
    {
        var props = _bodyProperties ?? Physics.BodyProperties.Default;
        _bodyProperties = props with 
        { 
            MaxLinearVelocity = Math.Max(0f, maxLinearVelocity),
            MaxAngularVelocity = Math.Max(0f, maxAngularVelocity)
        };
        OnBodyPropertiesChanged();
    }
    
    private void OnBodyPropertiesChanged()
    {
        if (_physicsWorld != null && BodyHandle.Value >= 0 && _bodyProperties.HasValue)
        {
            _physicsWorld.RegisterBodyProperties(BodyHandle, _bodyProperties.Value);
        }
    }
    
    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        float sqw = q.W * q.W;
        float sqx = q.X * q.X;
        float sqy = q.Y * q.Y;
        float sqz = q.Z * q.Z;
        float unit = sqx + sqy + sqz + sqw;
        float test = q.X * q.Y + q.Z * q.W;
        
        Vector3 euler;
        
        if (test > 0.499f * unit)
        {
            euler.Y = 2f * MathF.Atan2(q.X, q.W);
            euler.X = MathF.PI / 2f;
            euler.Z = 0f;
        }
        else if (test < -0.499f * unit)
        {
            euler.Y = -2f * MathF.Atan2(q.X, q.W);
            euler.X = -MathF.PI / 2f;
            euler.Z = 0f;
        }
        else
        {
            euler.Y = MathF.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);
            euler.X = MathF.Asin(2f * test / unit);
            euler.Z = MathF.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);
        }
        
        return euler;
    }
}