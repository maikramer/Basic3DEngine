using System.Numerics;

namespace Basic3DEngine.Entities.AI;

/// <summary>
/// Driver de IA simples que segue uma lista de waypoints, gerando comandos para o VehicleControllerComponent.
/// </summary>
public sealed class AIDriverComponent : Component
{
    public List<Vector3> Waypoints { get; } = new();
    public float WaypointReachRadius { get; set; } = 2.0f;
    public int CurrentIndex { get; private set; } = 0;
    public float TargetSpeed { get; set; } = 15f;
    public float SteeringGain { get; set; } = 2.0f; // ganho para converter erro angular em steer

    public override void Update(float deltaTime)
    {
        if (GameObject == null || Waypoints.Count == 0) return;

        var controller = GameObject.GetComponent<VehicleControllerComponent>();
        if (controller == null) return;

        controller.IsPlayerControlled = false;

        var pos = GameObject.Position;
        var target = Waypoints[CurrentIndex];
        var toTarget = target - pos;
        toTarget.Y = 0f;
        var distance = toTarget.Length();
        if (distance < WaypointReachRadius)
        {
            CurrentIndex = (CurrentIndex + 1) % Waypoints.Count;
            return;
        }

        var desiredYaw = MathF.Atan2(toTarget.Z, toTarget.X);
        var currentYaw = GameObject.Rotation.Y;
        var angleError = NormalizeAngle(desiredYaw - currentYaw);

        // Estimar steer na faixa [-1,1]
        var steer = Clamp(angleError * SteeringGain, -1f, 1f);

        // Para definir comandos no VehicleController, utilizamos a leitura do próprio componente
        // via campos privados. Como não temos API, simulamos empurrando torque diretamente:
        // usamos a direção do VehicleController via AddTorque/Force já implementados.
        // Estratégia: delegamos ao VehicleController usando o modelo de inputs (WAD), mas
        // como ele lê InputService, não há API. Então aplicamos física diretamente:
        // 1) torque para reduzir erro angular; 2) força para alcançar TargetSpeed.

        var rb = GameObject.GetComponent<Basic3DEngine.Physics.RigidbodyComponent>();
        if (rb == null) return;

        rb.AddTorque(new Vector3(0f, steer * controller.SteeringTorque, 0f));

        // Controle de velocidade proporcional simples
        var vel = rb.LinearVelocity; vel.Y = 0f;
        var speed = vel.Length();
        var speedErr = TargetSpeed - speed;
        var throttleForce = Clamp(speedErr, -controller.BrakeForce, controller.EngineForce);

        var forward = new Vector3(MathF.Cos(currentYaw), 0f, MathF.Sin(currentYaw));
        rb.AddForce(forward * throttleForce);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }
}


