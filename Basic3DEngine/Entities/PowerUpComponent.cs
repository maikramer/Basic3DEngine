using System.Numerics;
using Basic3DEngine.Physics;
using Basic3DEngine;

namespace Basic3DEngine.Entities;

/// <summary>
/// Power-up simples acionado por proximidade (AABB). Ao coletar, aplica um efeito ao alvo.
/// </summary>
public sealed class PowerUpComponent : Component
{
    public enum PowerUpType
    {
        SpeedBoost,
        JumpImpulse,
    }

    public PowerUpType Type { get; set; } = PowerUpType.SpeedBoost;
    public float Amount { get; set; } = 1.5f; // Para SpeedBoost: multiplicador
    public float Duration { get; set; } = 3f; // Duração do efeito
    public Vector3 Size { get; set; } = new Vector3(1f, 1f, 1f); // AABB local
    public string TargetTag { get; set; } = "Player";
    public bool Collected { get; private set; }
    public bool DestroyOnCollect { get; set; } = true;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (Collected || GameObject == null || GameObject.Enabled == false) return;

        // Checar proximidade com alvo por tag (AABB simples no mundo)
        var engine = FindEngine();
        if (engine == null) return;

        var candidates = engine.FindGameObjectsWithTag(TargetTag);
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            if (IsOverlappingAABB(GameObject.Position, Size, candidate.Position, new Vector3(1f)))
            {
                ApplyEffect(candidate);
                Collected = true;
                if (DestroyOnCollect)
                {
                    engine.RemoveGameObject(GameObject);
                }
                break;
            }
        }
    }

    private void ApplyEffect(GameObject target)
    {
        switch (Type)
        {
            case PowerUpType.SpeedBoost:
                var controller = target.GetComponent<VehicleControllerComponent>();
                controller?.ApplySpeedBoost(MathF.Max(Amount, 1f), MathF.Max(Duration, 0.1f));
                break;
            case PowerUpType.JumpImpulse:
                var rb = target.GetComponent<RigidbodyComponent>();
                rb?.AddImpulse(new Vector3(0, Amount, 0));
                break;
        }
    }

    private static bool IsOverlappingAABB(Vector3 centerA, Vector3 sizeA, Vector3 centerB, Vector3 sizeB)
    {
        var halfA = sizeA * 0.5f;
        var halfB = sizeB * 0.5f;
        return MathF.Abs(centerA.X - centerB.X) <= (halfA.X + halfB.X)
            && MathF.Abs(centerA.Y - centerB.Y) <= (halfA.Y + halfB.Y)
            && MathF.Abs(centerA.Z - centerB.Z) <= (halfA.Z + halfB.Z);
    }

    private Engine? FindEngine()
    {
        // Percurso simples: como não temos referência direta, obter a partir de um objeto conhecido
        // Heurística: o skybox é inserido no índice 0. Usaremos isso para obter a engine via componente.
        // Como alternativa mais segura, o projeto poderia injetar o contexto da engine em GameObject, mas manteremos simples aqui.
        return _engineResolver ??= EngineSingleton.Instance;
    }

    private Engine? _engineResolver;
}

/// <summary>
/// Singleton simples para expor a referência da Engine às Components que precisam (até termos um contexto melhor).
/// </summary>
// EngineSingleton movido para Core/EngineSingleton.cs


