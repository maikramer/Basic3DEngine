using System.Numerics;
using Basic3DEngine.Services;
using Basic3DEngine.Physics;
using Basic3DEngine;

namespace Basic3DEngine.Entities;

/// <summary>
/// Faz a câmera da engine seguir este GameObject com um offset suave (câmera de perseguição de carro).
/// </summary>
public sealed class FollowCameraComponent : Component
{
    public float DistanceBack { get; set; } = 12f;
    public float Height { get; set; } = 5f;
    public float LateralOffset { get; set; } = 0f;
    public float SmoothFactor { get; set; } = 10f; // maior = aproxima mais rápido
    public bool AlignToForward { get; set; } = true;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (GameObject == null) return;
        var engine = EngineSingleton.Instance;
        if (engine == null) return;

        var yaw = GameObject.Rotation.Y;
        var forward = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
        var right = new Vector3(-forward.Z, 0f, forward.X);
        // Usar a pose do rigidbody quando disponível para reduzir oscilação entre física e visual
        var rb = GameObject.GetComponent<RigidbodyComponent>();
        var basePos = rb != null ? rb.Pose.Position : GameObject.Position;
        var desiredPosition = basePos - forward * DistanceBack + new Vector3(0, Height, 0) + right * LateralOffset;
        var camera = engine.Camera;
        var current = camera.Position;
        var t = 1f - MathF.Exp(-SmoothFactor * deltaTime);
        var newPos = Vector3.Lerp(current, desiredPosition, t);
        camera.Position = newPos;

        if (AlignToForward)
        {
            var lookBase = rb != null ? rb.Pose.Position : GameObject.Position;
            var lookTarget = lookBase + forward * 10f;
            camera.LookAt(newPos, lookTarget);
        }
    }
}


