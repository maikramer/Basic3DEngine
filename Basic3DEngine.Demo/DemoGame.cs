using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Physics.Shapes;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Demo;

public class DemoGame
{
    private readonly Engine _engine;
    private readonly List<GameObject> _demoObjects = new();
    private float _demoTimer = 0f;
    private int _currentDemo = 0;
    private bool _initialized = false;

    public DemoGame(Engine engine)
    {
        _engine = engine;
        
        // Aguardar a inicialização da engine antes de configurar a física
        // A configuração será feita no Update quando a física estiver disponível
    }
    
    private void CreateGround(Engine engine)
    {
        var ground = engine.CreatePhysicsGameObject(
            "Ground",
            new Vector3(0, -1, 0),
            new BoxShape(new Vector3(20, 1, 20)),
            Material.Concrete,
            0f // massa 0 para objeto estático
        );
        
        var groundRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.5f, 0.5f, 0.5f, 1f));
        ground.AddComponent(groundRenderer);
        ground.Tag = "Static";
    }
    
    private void CreateWalls(Engine engine)
    {
        // Parede traseira
        var backWall = engine.CreatePhysicsGameObject(
            "BackWall",
            new Vector3(0, 5, -10),
            new BoxShape(new Vector3(20, 10, 1)),
            Material.Concrete,
            0f
        );
        var backWallRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.3f, 0.3f, 1f));
        backWall.AddComponent(backWallRenderer);
        backWall.Tag = "Static";
        
        // Paredes laterais
        var leftWall = engine.CreatePhysicsGameObject(
            "LeftWall",
            new Vector3(-10, 5, 0),
            new BoxShape(new Vector3(1, 10, 20)),
            Material.Concrete,
            0f
        );
        var leftWallRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.3f, 0.3f, 1f));
        leftWall.AddComponent(leftWallRenderer);
        leftWall.Tag = "Static";
        
        var rightWall = engine.CreatePhysicsGameObject(
            "RightWall",
            new Vector3(10, 5, 0),
            new BoxShape(new Vector3(1, 10, 20)),
            Material.Concrete,
            0f
        );
        var rightWallRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.3f, 0.3f, 1f));
        rightWall.AddComponent(rightWallRenderer);
        rightWall.Tag = "Static";
    }
    
    private void CreateRamps(Engine engine)
    {
        // Rampa de metal (baixo atrito)
        var metalRamp = engine.CreatePhysicsGameObject(
            "MetalRamp",
            new Vector3(-6, 2, 5),
            new BoxShape(new Vector3(3, 0.2f, 6)),
            Material.Metal,
            0f
        );
        metalRamp.Rotation = new Vector3(20f * MathF.PI / 180f, 0, 0); // 20 graus
        var metalRampRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.8f, 0.8f, 0.9f, 1f));
        metalRamp.AddComponent(metalRampRenderer);
        metalRamp.Tag = "Static";
        
        // Rampa de madeira (atrito médio)
        var woodRamp = engine.CreatePhysicsGameObject(
            "WoodRamp",
            new Vector3(0, 2, 5),
            new BoxShape(new Vector3(3, 0.2f, 6)),
            Material.Wood,
            0f
        );
        woodRamp.Rotation = new Vector3(20f * MathF.PI / 180f, 0, 0);
        var woodRampRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        woodRamp.AddComponent(woodRampRenderer);
        woodRamp.Tag = "Static";
        
        // Rampa de borracha (alto atrito)
        var rubberRamp = engine.CreatePhysicsGameObject(
            "RubberRamp",
            new Vector3(6, 2, 5),
            new BoxShape(new Vector3(3, 0.2f, 6)),
            Material.Rubber,
            0f
        );
        rubberRamp.Rotation = new Vector3(20f * MathF.PI / 180f, 0, 0);
        var rubberRampRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.2f, 0.2f, 0.2f, 1f));
        rubberRamp.AddComponent(rubberRampRenderer);
        rubberRamp.Tag = "Static";
    }
    
    private void CreateSimpleGround(Engine engine)
    {
        LoggingService.LogInfo("Creating simple ground for basic physics test");
        
        var ground = engine.CreatePhysicsGameObject(
            "SimpleGround",
            new Vector3(0, 0, 0),
            new BoxShape(new Vector3(20, 2, 20)),
            Material.Default,
            0f
        );
        
        // Forçar o rigidbody a ser estático manualmente
        var rigidbody = ground.GetComponent<RigidbodyComponent>();
        if (rigidbody != null)
        {
            LoggingService.LogInfo($"Ground rigidbody created - IsStatic: {rigidbody.IsStatic}");
        }
        
        var groundRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.6f, 0.3f, 1f));
        ground.AddComponent(groundRenderer);
        ground.Tag = "Static";
        
        LoggingService.LogInfo("Simple ground created successfully");
    }
    
    private void CreateTestSphere(Engine engine)
    {
        LoggingService.LogInfo("Creating test sphere for physics test");
        
        var testSphere = engine.CreatePhysicsGameObject(
            "TestSphere",
            new Vector3(0, 10, 0),
            new SphereShape(1f),
            Material.Default,
            2f
        );
        
        var sphereRenderer = engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 0f, 1f));
        testSphere.AddComponent(sphereRenderer);
        testSphere.Tag = "DemoObject";
        
        LoggingService.LogInfo("Test sphere created successfully");
    }
    
    private void CreateDampingDemo(Engine engine)
    {
        CleanupDemoObjects();
        
        float y = 15f; // Altura maior para demonstrar o efeito
        
        // Três esferas com diferentes comportamentos (simulados com física básica)
        
        // Esfera 1: Básica
        var sphere1 = engine.CreatePhysicsGameObject(
            "Sphere_Basic1",
            new Vector3(-4, y, 2),
            new SphereShape(1f),
            Material.Metal,
            2f
        );
        var renderer1 = engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 0f, 1f));
        sphere1.AddComponent(renderer1);
        sphere1.Tag = "DemoObject";
        
        // Esfera 2: Básica
        var sphere2 = engine.CreatePhysicsGameObject(
            "Sphere_Basic2",
            new Vector3(0, y, 2),
            new SphereShape(1f),
            Material.Metal,
            2f
        );
        var renderer2 = engine.CreateSphereRenderer(new RgbaFloat(0f, 1f, 0f, 1f));
        sphere2.AddComponent(renderer2);
        sphere2.Tag = "DemoObject";
        
        // Esfera 3: Básica
        var sphere3 = engine.CreatePhysicsGameObject(
            "Sphere_Basic3",
            new Vector3(4, y, 2),
            new SphereShape(1f),
            Material.Metal,
            2f
        );
        var renderer3 = engine.CreateSphereRenderer(new RgbaFloat(0f, 0f, 1f, 1f));
        sphere3.AddComponent(renderer3);
        sphere3.Tag = "DemoObject";
        
        // Aplicar velocidade inicial para demonstrar o movimento
        var rigidbody1 = sphere1.GetComponent<RigidbodyComponent>();
        var rigidbody2 = sphere2.GetComponent<RigidbodyComponent>();
        var rigidbody3 = sphere3.GetComponent<RigidbodyComponent>();
        
        rigidbody1?.SetVelocity(new Vector3(0, 0, 0), Vector3.Zero);
        rigidbody2?.SetVelocity(new Vector3(0, 0, 0), Vector3.Zero);
        rigidbody3?.SetVelocity(new Vector3(0, 0, 0), Vector3.Zero);
        
        _demoObjects.AddRange(new[] { sphere1, sphere2, sphere3 });
        
        LoggingService.LogInfo("Basic physics demo: Three spheres with basic physics behavior");
    }
    
    public void Update(float deltaTime)
    {
        // Inicializar na primeira execução quando a física estiver disponível
        if (!_initialized && _engine.PhysicsWorld != null)
        {
            _initialized = true;
            
            // Configurar gravidade mais realista
            _engine.SetGravity(9.81f);
            
            // TESTE SIMPLES - apenas chão e uma esfera
            CreateSimpleGround(_engine);
            CreateTestSphere(_engine);
            
            LoggingService.LogInfo("DemoGame initialized successfully");
        }
        
        if (!_initialized) return; // Aguardar inicialização
        
        _demoTimer += deltaTime;
        
        // Trocar demo a cada 15 segundos - TEMPORARIAMENTE DESABILITADO
        /*
        if (_demoTimer >= 15f)
        {
            _demoTimer = 0f;
            _currentDemo = (_currentDemo + 1) % 3; // Ciclar entre 3 demos
            StartDemoSection(_currentDemo);
        }
        */
        
        // Log de debugging específico para o objeto de teste
        if (_demoTimer < 1f) // Só nos primeiros segundos
        {
            var testSphere = _engine.FindGameObject("TestSphere");
            if (testSphere != null)
            {
                LoggingService.LogDebug($"TestSphere Position: {testSphere.Position}");
            }
        }
    }
    
    private void StartDemoSection(int section)
    {
        LoggingService.LogInfo($"Starting demo section {section}");
        
        switch (section)
        {
            case 0:
                CreateDampingDemo(_engine);
                break;
            case 1:
                CreateMaterialDemo(_engine);
                break;
            case 2:
                CreateComplexDemo(_engine);
                break;
        }
    }
    
    private void CreateMaterialDemo(Engine engine)
    {
        CleanupDemoObjects();
        
        float y = 15f;
        
        // Cubos com diferentes materiais
        var metalCube = engine.CreatePhysicsGameObject(
            "MetalCube",
            new Vector3(-3, y, 0),
            new BoxShape(new Vector3(1, 1, 1)),
            Material.Metal,
            5f
        );
        var metalRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.8f, 0.8f, 0.9f, 1f));
        metalCube.AddComponent(metalRenderer);
        metalCube.Tag = "DemoObject";
        
        var woodCube = engine.CreatePhysicsGameObject(
            "WoodCube",
            new Vector3(0, y, 0),
            new BoxShape(new Vector3(1, 1, 1)),
            Material.Wood,
            3f
        );
        var woodRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        woodCube.AddComponent(woodRenderer);
        woodCube.Tag = "DemoObject";
        
        var rubberCube = engine.CreatePhysicsGameObject(
            "RubberCube",
            new Vector3(3, y, 0),
            new BoxShape(new Vector3(1, 1, 1)),
            Material.Rubber,
            2f
        );
        var rubberRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.2f, 0.2f, 0.2f, 1f));
        rubberCube.AddComponent(rubberRenderer);
        rubberCube.Tag = "DemoObject";
        
        _demoObjects.AddRange(new[] { metalCube, woodCube, rubberCube });
        
        LoggingService.LogInfo("Material demo: Different materials with different properties");
    }
    
    private void CreateComplexDemo(Engine engine)
    {
        CleanupDemoObjects();
        
        // Torre de caixas
        for (int i = 0; i < 5; i++)
        {
            var box = engine.CreatePhysicsGameObject(
                $"TowerBox_{i}",
                new Vector3(0, 2 + i * 2.1f, 0),
                new BoxShape(new Vector3(1, 1, 1)),
                Material.Wood,
                1f
            );
            var boxRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
            box.AddComponent(boxRenderer);
            box.Tag = "DemoObject";
            _demoObjects.Add(box);
        }
        
        // Projétil para derrubar a torre
        Task.Delay(2000).ContinueWith(_ =>
        {
            var projectile = engine.CreatePhysicsGameObject(
                "Projectile",
                new Vector3(-10, 8, 0),
                new SphereShape(0.5f),
                Material.Metal,
                3f
            );
            var projectileRenderer = engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 0f, 1f));
            projectile.AddComponent(projectileRenderer);
            projectile.Tag = "DemoObject";
            
            // Aplicar impulso
            var rigidbody = projectile.GetComponent<RigidbodyComponent>();
            rigidbody?.AddImpulse(new Vector3(15f, 0f, 0f));
            
            _demoObjects.Add(projectile);
        });
        
        LoggingService.LogInfo("Complex demo: Tower destruction with projectile");
    }
    
    private void CleanupDemoObjects()
    {
        foreach (var obj in _demoObjects)
        {
            _engine.RemoveGameObject(obj);
        }
        _demoObjects.Clear();
    }
} 