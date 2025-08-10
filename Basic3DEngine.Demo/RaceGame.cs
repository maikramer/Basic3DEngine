using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Entities;
using Basic3DEngine.Entities.AI;
using Basic3DEngine.Physics;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Demo;

public class RaceGame : Game
{
    private Engine _engine;
    private GameObject _playerCar;

    public override void Initialize(Engine engine)
    {
        _engine = engine;
        EngineSingleton.Instance = engine;

        // Setup básico
        _engine.SetGravity(9.81f);
        _engine.Lighting.SetupDefaultLighting();
        _engine.CreateSkybox();

        // Desativar controles FPS da câmera; a câmera será controlada pela follow-cam do carro
        _engine.EnableFPSCameraControls(false);

        // Afinar solver para maior estabilidade do carro
        _engine.PhysicsWorld?.SetSolverIterations(16, 3);

        // Pista simples: chão e muros
        var ground = _engine.CreateGroundLit(new Vector3(0, -0.5f, 0), new Vector3(80, 1, 50), new RgbaFloat(0.18f, 0.2f, 0.2f, 1f));
        // Aumentar atrito do chão
        var groundRb = ground.GetComponent<RigidbodyComponent>();
        if (groundRb != null)
        {
            groundRb.Material = Material.Concrete;
        }

        // Muros externos
        _engine.CreateStaticCubeLit("WallLeft", new Vector3(-40, 2, 0), new Vector3(1, 4, 50), new RgbaFloat(0.4f,0.4f,0.45f,1));
        _engine.CreateStaticCubeLit("WallRight", new Vector3(40, 2, 0), new Vector3(1, 4, 50), new RgbaFloat(0.4f,0.4f,0.45f,1));
        _engine.CreateStaticCubeLit("WallBack", new Vector3(0, 2, -25), new Vector3(80, 4, 1), new RgbaFloat(0.4f,0.4f,0.45f,1));
        _engine.CreateStaticCubeLit("WallFront", new Vector3(0, 2, 25), new Vector3(80, 4, 1), new RgbaFloat(0.4f,0.4f,0.45f,1));

        // Carro do jogador
        _playerCar = _engine.CreateCar("PlayerCar", new Vector3(-30, 2.0f, -20), new Vector3(2f, 1f, 4f), new RgbaFloat(0.9f, 0.1f, 0.1f, 1f), 800f);
        var playerRb = _playerCar.GetComponent<RigidbodyComponent>();
        if (playerRb != null)
        {
            playerRb.Material = Material.Rubber; // Alto atrito
            playerRb.SetVelocity(Vector3.Zero, Vector3.Zero);
        }
        _playerCar.Tag = "Player";
        var followCam = new FollowCameraComponent { DistanceBack = 14f, Height = 6f, SmoothFactor = 8f };
        _playerCar.AddComponent(followCam);

        // Power-ups
        CreatePowerUp(new Vector3(-10, 0.5f, 0));
        CreatePowerUp(new Vector3(10, 0.5f, 10));
        CreatePowerUp(new Vector3(20, 0.5f, -10));

        // Inimigo IA
        var aiCar = _engine.CreateCar("AICar", new Vector3(-28, 2.0f, -20), new Vector3(2f, 1f, 4f), new RgbaFloat(0.1f, 0.1f, 0.9f, 1f), 800f);
        var aiDriver = new AIDriverComponent
        {
            TargetSpeed = 14f,
            SteeringGain = 2.5f,
            WaypointReachRadius = 3f
        };
        aiDriver.Waypoints.AddRange(new []
        {
            new Vector3(-30, 0, -20), new Vector3(-20, 0, -10), new Vector3(-10, 0, 0), new Vector3(0, 0, 10),
            new Vector3(15, 0, 15), new Vector3(25, 0, 5), new Vector3(30, 0, -10), new Vector3(15, 0, -20),
            new Vector3(0, 0, -18), new Vector3(-15, 0, -20)
        });
        aiCar.AddComponent(aiDriver);
    }

    public override void Update(float deltaTime)
    {
        // Reset posição player
        if (InputService.IsKeyPressed(Key.R))
        {
            var rb = _playerCar.GetComponent<RigidbodyComponent>();
            if (rb != null)
            {
                rb.Pose = new BepuPhysics.RigidPose(new Vector3(-30, 1, -20), System.Numerics.Quaternion.Identity);
                rb.SetVelocity(Vector3.Zero, Vector3.Zero);
            }
        }
    }

    private void CreatePowerUp(Vector3 position)
    {
        var go = new GameObject("PowerUp") { Position = position, Scale = new Vector3(1,1,1) };
        var renderer = _engine.CreateCubeRenderer(new RgbaFloat(1f, 0.85f, 0.2f, 1f));
        go.AddComponent(renderer);
        var power = new PowerUpComponent
        {
            Type = PowerUpComponent.PowerUpType.SpeedBoost,
            Amount = 1.8f,
            Duration = 3f,
            Size = new Vector3(1.2f, 1.2f, 1.2f),
            TargetTag = "Player"
        };
        go.AddComponent(power);
        _engine.AddGameObject(go);
    }
}

public class RaceGameWrapper : Game
{
    private RaceGame _race;
    public RaceGameWrapper(Engine engine)
    {
        _race = new RaceGame();
    }
    public override void Initialize(Engine engine)
    {
        _race.Initialize(engine);
    }
    public override void Update(float deltaTime)
    {
        _race.Update(deltaTime);
    }
}


