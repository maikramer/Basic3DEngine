using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Entities;
using Basic3DEngine.Services;
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
            // Sincroniza dados locais com o corpo físico
            var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
            _pose = bodyRef.Pose;
            _linearVelocity = bodyRef.Velocity.Linear;
            _angularVelocity = bodyRef.Velocity.Angular;
        }
    }

    /// <summary>
    /// Sincroniza a posição e rotação do GameObject a partir do estado atual do corpo físico.
    /// Chame isto após o passo de física para refletir o estado mais recente antes de renderizar.
    /// </summary>
    public void SyncFromPhysics()
    {
        if (IsStatic || _physicsWorld == null || GameObject == null || BodyHandle.Value < 0) return;
        var bodyRef = _physicsWorld.GetBodyReference(BodyHandle);
        var pose = bodyRef.Pose;
        if (float.IsNaN(pose.Position.X) || float.IsNaN(pose.Position.Y) || float.IsNaN(pose.Position.Z))
        {
            LoggingService.LogError("Rigidbody has invalid position (NaN)");
            return;
        }
        GameObject.Position = pose.Position;
        GameObject.Rotation = QuaternionToEuler(pose.Orientation);
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