using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Veldrid;

namespace Basic3DEngine.Demo;

public class DemoGame : Game
{
    private readonly Random _random = new();
    
    public override void Initialize(Engine engine)
    {
        _engine = engine;
        
        // Criar o chão (objeto estático) com material de madeira
        var groundObject = new GameObject("Ground")
        {
            Position = new Vector3(0, -1, 0),
            Scale = new Vector3(10, 0.1f, 10)
        };

        var groundRenderComponent = engine.CreateCubeRenderer(RgbaFloat.Grey);
        groundObject.AddComponent(groundRenderComponent);

        var groundRigidbody = engine.CreateRigidbody(0, true);
        groundRigidbody.Material = Material.Wood;
        groundObject.AddComponent(groundRigidbody);

        engine.AddBoxPhysics(groundRigidbody, groundObject.Scale);
        AddGameObject(groundObject);

        // Criar alguns cubos caindo com diferentes materiais
        for (var i = 0; i < 5; i++)
        {
            var position = new Vector3(
                (float)(_random.NextDouble() * 4 - 2), // -2 to 2 range in X
                5 + i * 2, // Staggered heights
                (float)(_random.NextDouble() * 4 - 2) // -2 to 2 range in Z
            );

            // Alternar entre diferentes materiais
            Material material = (i % 4) switch
            {
                0 => Material.Wood,
                1 => Material.Metal,
                2 => Material.Rubber,
                _ => Material.Ice
            };

            var cubeObject = new GameObject($"Cube {i}")
            {
                Position = position
            };

            var cubeRenderComponent = engine.CreateCubeRenderer(new RgbaFloat(
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                1.0f));
            cubeObject.AddComponent(cubeRenderComponent);

            var cubeRigidbody = engine.CreateRigidbody();
            cubeRigidbody.Material = material;
            cubeObject.AddComponent(cubeRigidbody);

            engine.AddBoxPhysics(cubeRigidbody, Vector3.One);
            AddGameObject(cubeObject);
        }

        // Criar algumas esferas caindo com diferentes materiais
        for (var i = 0; i < 3; i++)
        {
            var position = new Vector3(
                (float)(_random.NextDouble() * 4 - 2), // -2 to 2 range in X
                10 + i * 2, // Staggered heights
                (float)(_random.NextDouble() * 4 - 2) // -2 to 2 range in Z
            );

            // Alternar entre diferentes materiais
            Material material = (i % 4) switch
            {
                0 => Material.Wood,
                1 => Material.Metal,
                2 => Material.Rubber,
                _ => Material.Ice
            };

            var sphereObject = new GameObject($"Sphere {i}")
            {
                Position = position
            };

            var sphereRenderComponent = engine.CreateSphereRenderer(new RgbaFloat(
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                1.0f));
            sphereObject.AddComponent(sphereRenderComponent);

            var sphereRigidbody = engine.CreateRigidbody();
            sphereRigidbody.Material = material;
            sphereObject.AddComponent(sphereRigidbody);

            engine.AddSpherePhysics(sphereRigidbody, 0.5f);
            AddGameObject(sphereObject);
        }
    }

    public override void Update(float deltaTime)
    {
        // A lógica específica do jogo vai aqui
        // Por enquanto, toda a lógica está sendo tratada pela engine e componentes
    }
    
    public override void Shutdown()
    {
        // Limpeza específica do jogo, se necessária
    }
} 