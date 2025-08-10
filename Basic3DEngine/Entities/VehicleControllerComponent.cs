using System.Numerics;
using Basic3DEngine.Physics;
using Basic3DEngine.Services;
using Basic3DEngine;
using Veldrid;

namespace Basic3DEngine.Entities;

/// <summary>
/// Controlador simples de veículo baseado em física (carro estilo arcade).
/// Aplica forças no <see cref="RigidbodyComponent"/> para acelerar, frear e esterçar.
///</summary>
public sealed class VehicleControllerComponent : Component
{
    private readonly RigidbodyComponent _rigidbody;

    // Configurações
    public float EngineForce { get; set; } = 6000f;    // Força de aceleração (ajustado p/ massa alta)
    public float BrakeForce { get; set; } = 10000f;    // Força de frenagem
    public float MaxSpeed { get; set; } = 30f;         // Velocidade máxima (m/s)
    public float SteeringTorque { get; set; } = 2000f; // Torque de direção (eixo Y)
    public float SteeringSpeed { get; set; } = 1.8f;  // Velocidade de rotação (rad/s) para steering cinemático
    public float LateralFriction { get; set; } = 12f; // Força para reduzir derrapagem lateral
    public float DownforceCoefficient { get; set; } = 200f; // Força p/ baixo proporcional à velocidade
    public float StabilizationTorque { get; set; } = 1200f; // Torque suave para reduzir pitch/roll
    public bool UseKinematicSteering { get; set; } = true; // Direção ajustando yaw diretamente

    // Boost
    private float _speedMultiplier = 1f;
    private float _boostTimer = 0f;

    public bool IsPlayerControlled { get; set; } = true;

    public VehicleControllerComponent(RigidbodyComponent rigidbody)
    {
        _rigidbody = rigidbody;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (!Enabled || GameObject == null) return;

        // Atualizar boost
        if (_boostTimer > 0f)
        {
            _boostTimer -= deltaTime;
            if (_boostTimer <= 0f)
            {
                _speedMultiplier = 1f;
                _boostTimer = 0f;
            }
        }

        // Vetores de referência (forward derivado do yaw)
        var yaw = GameObject.Rotation.Y;
        var forward = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        var right = new Vector3(-forward.Z, 0f, forward.X);

        // Controle do jogador (W/S aceleração, A/D direção, Espaço freio, setas também)
        float throttle = 0f;
        float steer = 0f;
        bool braking = false;

        if (IsPlayerControlled)
        {
            if (InputService.IsKeyDown(Key.W) || InputService.IsKeyDown(Key.Up)) throttle += 1f;
            if (InputService.IsKeyDown(Key.S) || InputService.IsKeyDown(Key.Down)) throttle -= 1f;
            if (InputService.IsKeyDown(Key.A) || InputService.IsKeyDown(Key.Left)) steer -= 1f;
            if (InputService.IsKeyDown(Key.D) || InputService.IsKeyDown(Key.Right)) steer += 1f;
            braking = InputService.IsKeyDown(Key.Space);
        }

        // Limitar velocidade
        var velocity = _rigidbody.LinearVelocity;
        var speed = velocity.Length();
        var maxAllowed = MaxSpeed * _speedMultiplier;
        if (speed > maxAllowed)
        {
            // Aplicar leve freio aerodinâmico
            var excess = speed - maxAllowed;
            var drag = -Vector3.Normalize(velocity) * MathF.Min(excess * 2f, 10f) * deltaTime;
            _rigidbody.AddForce(drag);
        }

        // Aceleração
        if (MathF.Abs(throttle) > 0.01f)
        {
            var force = forward * (throttle > 0 ? EngineForce : -EngineForce) * _speedMultiplier;
            _rigidbody.AddForce(force);
        }

        // Frenagem forte
        if (braking)
        {
            var brake = -velocity * MathF.Min(1f, BrakeForce * deltaTime);
            brake.Y = 0f;
            _rigidbody.AddForce(brake);
        }

        // Direção
        if (MathF.Abs(steer) > 0.01f)
        {
            if (UseKinematicSteering)
            {
                // Ajusta yaw diretamente para resposta mais estável e previsível
                var speedFactor = 0.5f + MathF.Min(1f, speed / 10f);
                var yawDelta = steer * SteeringSpeed * speedFactor * deltaTime;
                var newYaw = yaw + yawDelta;
                // Atualiza pose do rigidbody e rotação do GameObject
                var pose = _rigidbody.Pose;
                pose.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, newYaw);
                _rigidbody.Pose = pose;
                GameObject.Rotation = new Vector3(0f, newYaw, 0f);
                // Zerar velocidade angular Y para evitar drift
                var av = _rigidbody.AngularVelocity;
                av.Y = 0f;
                _rigidbody.AngularVelocity = av;
                // Recalcular forward/right
                forward = new Vector3(MathF.Cos(newYaw), 0f, MathF.Sin(newYaw));
                right = new Vector3(-forward.Z, 0f, forward.X);
            }
            else
            {
                var torque = new Vector3(0f, steer * SteeringTorque, 0f);
                _rigidbody.AddTorque(torque);
            }
        }

        // Atrito lateral: remover componente perpendicular ao forward
        // Recalcular vel/dir depois de updates de torque/força
        velocity = _rigidbody.LinearVelocity;
        var lateral = Vector3.Dot(velocity, right) * right;
        var lateralCorrection = -lateral * MathF.Min(1f, LateralFriction * deltaTime);
        _rigidbody.AddForce(lateralCorrection);

        // Downforce simples proporcional à velocidade para colar no chão
        // Aplicar downforce apenas se próximo ao chão (raycast simples)
        var engine = EngineSingleton.Instance;
        if (engine != null)
        {
            if (engine.Raycast(GameObject.Position + new Vector3(0, 0.6f, 0), -Vector3.UnitY, 1.2f, out var hit))
            {
                var downforce = -Vector3.UnitY * (DownforceCoefficient * velocity.Length());
                _rigidbody.AddForce(downforce);
            }
        }

        // Estabilização de pitch/roll (aplica torque contrário a X/Z)
        var pitch = GameObject.Rotation.X;
        var roll = GameObject.Rotation.Z;
        var stabilizeTorque = new Vector3(-pitch * StabilizationTorque, 0f, -roll * StabilizationTorque);
        _rigidbody.AddTorque(stabilizeTorque);

        // Trava parcial de rotação em X/Z para evitar balançar excessivo
        var angVel = _rigidbody.AngularVelocity;
        angVel.X *= 0.2f; // amortecer em vez de zerar para evitar jitter
        angVel.Z *= 0.2f;
        _rigidbody.AngularVelocity = angVel;
    }

    /// <summary>
    /// Aplica um boost de velocidade temporário.
    /// </summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = MathF.Max(_speedMultiplier, multiplier);
        _boostTimer = MathF.Max(_boostTimer, duration);
    }
}


