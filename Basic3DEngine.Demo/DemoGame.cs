using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Core;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Demo;

public class DemoGame : Game
{
    private readonly Random _random = new();
    private float _timeSinceLastBall;
    private float _timeSinceLastSpawn;
    private int _demoSection;
    private float _sectionTimer;
    
    public override void Initialize(Engine engine)
    {
        _engine = engine;
        
        // Ativar física avançada para demonstrar todas as capacidades
        engine.EnableAdvancedPhysics();
        
        // Configurar gravidade mais realista
        engine.SetGravity(9.81f);
        
        // TESTE SIMPLES - apenas chão e uma esfera
        CreateSimpleGround(engine);
        CreateTestSphere(engine);
        
        // Comentar o resto por enquanto
        /*
        // Criar o cenário base
        CreateGround(engine);
        CreateWalls(engine);
        
        // Criar rampas para demonstrar atrito
        CreateRamps(engine);
        
        // Iniciar com a primeira seção da demo
        StartDemoSection(0);
        */
    }
    
    private void CreateGround(Engine engine)
    {
        // Chão principal com material concreto - tamanho normal
        var groundObject = new GameObject("Ground")
        {
            Position = new Vector3(0, -1, 0),
            Scale = new Vector3(20, 1, 20), // Tamanho mais normal
            Tag = "Ground"
        };

        var groundRenderComponent = engine.CreateCubeRenderer(new RgbaFloat(0.5f, 0.5f, 0.5f, 1f));
        groundObject.AddComponent(groundRenderComponent);

        var groundRigidbody = engine.CreateRigidbody(0, true);
        groundRigidbody.Material = Material.Concrete;
        groundRigidbody.Shape = engine.CreateBoxShape(groundObject.Scale);
        groundObject.AddComponent(groundRigidbody);

        engine.PhysicsWorld?.AddBody(groundRigidbody);
        AddGameObject(groundObject);
    }

    private void CreateWalls(Engine engine)
    {
        // Paredes de vidro menores e mais visíveis
        CreateWall(engine, new Vector3(0, 3, -12), new Vector3(20, 6, 1), "FrontWall");
        CreateWall(engine, new Vector3(0, 3, 12), new Vector3(20, 6, 1), "BackWall");
        CreateWall(engine, new Vector3(-12, 3, 0), new Vector3(1, 6, 20), "LeftWall");
        CreateWall(engine, new Vector3(12, 3, 0), new Vector3(1, 6, 20), "RightWall");
    }
    
    private void CreateWall(Engine engine, Vector3 position, Vector3 scale, string name)
    {
        var wall = new GameObject(name)
        {
            Position = position,
            Scale = scale,
            Tag = "Wall"
        };
        
        // Tornar as paredes visíveis
        var wallRenderer = engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.3f, 0.8f, 0.5f));
        wall.AddComponent(wallRenderer);
        
        var wallRigidbody = engine.CreateRigidbody(0, true);
        wallRigidbody.Material = Material.Glass;
        wallRigidbody.Shape = engine.CreateBoxShape(scale);
        wall.AddComponent(wallRigidbody);
        
        engine.PhysicsWorld?.AddBody(wallRigidbody);
        AddGameObject(wall);
    }
    
    private void CreateRamps(Engine engine)
    {
        // Rampa 1: Material Ice (muito escorregadio)
        CreateRamp(engine, new Vector3(-8, 2, -5), 15f, Material.IceSlippery, 
            new RgbaFloat(0.8f, 0.9f, 1f, 1f), "IceRamp");
        
        // Rampa 2: Material WoodRough (alto atrito)
        CreateRamp(engine, new Vector3(0, 2, -5), 15f, Material.WoodRough, 
            new RgbaFloat(0.4f, 0.2f, 0.1f, 1f), "WoodRamp");
        
        // Rampa 3: Material MetalSmooth (médio atrito)
        CreateRamp(engine, new Vector3(8, 2, -5), 15f, Material.MetalSmooth, 
            new RgbaFloat(0.8f, 0.8f, 0.9f, 1f), "MetalRamp");
    }
    
    private void CreateRamp(Engine engine, Vector3 position, float angle, Material material, RgbaFloat color, string name)
    {
        var ramp = new GameObject(name)
        {
            Position = position,
            Scale = new Vector3(4f, 0.5f, 8f), // Rampas maiores
            Rotation = new Vector3(0, 0, -angle * MathF.PI / 180f),
            Tag = "Ramp"
        };
        
        var rampRenderer = engine.CreateCubeRenderer(color);
        ramp.AddComponent(rampRenderer);
        
        var rampRigidbody = engine.CreateRigidbody(0, true);
        rampRigidbody.Material = material;
        rampRigidbody.Shape = engine.CreateBoxShape(ramp.Scale);
        rampRigidbody.Pose = new BepuPhysics.RigidPose(
            position, 
            Quaternion.CreateFromYawPitchRoll(0, 0, -angle * MathF.PI / 180f)
        );
        ramp.AddComponent(rampRigidbody);
        
        engine.PhysicsWorld?.AddBody(rampRigidbody);
        AddGameObject(ramp);
    }
    
    private void CreateSimpleGround(Engine engine)
    {
        // Chão muito simples e grande
        var groundObject = new GameObject("SimpleGround")
        {
            Position = new Vector3(0, -2, 0),
            Scale = new Vector3(10, 1, 10),
            Tag = "Ground"
        };

        var groundRenderComponent = engine.CreateCubeRenderer(new RgbaFloat(0.8f, 0.8f, 0.8f, 1f));
        groundObject.AddComponent(groundRenderComponent);

        var groundRigidbody = engine.CreateRigidbody(0, true);
        groundRigidbody.Material = Material.Concrete;
        groundRigidbody.Shape = engine.CreateBoxShape(groundObject.Scale);
        
        // IMPORTANTE: Definir a pose explicitamente
        groundRigidbody.Pose = new BepuPhysics.RigidPose(groundObject.Position, Quaternion.Identity);
        
        groundObject.AddComponent(groundRigidbody);

        engine.PhysicsWorld?.AddBody(groundRigidbody);
        AddGameObject(groundObject);
    }
    
    private void CreateTestSphere(Engine engine)
    {
        // Esfera de teste simples - VERSÃO CORRIGIDA
        var sphereObject = new GameObject("TestSphere")
        {
            Position = new Vector3(0, 5, 0),
            Tag = "TestObject"
        };

        var sphereRenderer = engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 0f, 1f));
        sphereObject.AddComponent(sphereRenderer);

        var sphereRigidbody = engine.CreateRigidbody(1f, false); // Massa explícita de 1kg
        sphereRigidbody.Material = Material.Metal;
        sphereRigidbody.Shape = engine.CreateSphereShape(0.5f);
        
        // IMPORTANTE: Definir pose normalizada explicitamente
        sphereRigidbody.Pose = new BepuPhysics.RigidPose(
            new Vector3(0, 5, 0), 
            Quaternion.Identity
        );
        
        sphereObject.AddComponent(sphereRigidbody);

        engine.PhysicsWorld?.AddBody(sphereRigidbody);
        AddGameObject(sphereObject);
    }
    
    private void StartDemoSection(int section)
    {
        _demoSection = section;
        _sectionTimer = 0;
        
        // Limpar objetos dinâmicos existentes
        CleanupDynamicObjects();
        
        switch (section)
        {
            case 0:
                // Seção 1: Demonstração de amortecimento (damping)
                DemoDampingSection();
                break;
                
            case 1:
                // Seção 2: Demonstração de atrito estático vs dinâmico
                DemoFrictionSection();
                break;
                
            case 2:
                // Seção 3: Demonstração de restituição (bouncing)
                DemoRestitutionSection();
                break;
                
            case 3:
                // Seção 4: Demonstração de conservação de momento angular
                DemoAngularMomentumSection();
                break;
                
            case 4:
                // Seção 5: Demonstração de propriedades especiais
                DemoSpecialPropertiesSection();
                break;
        }
    }
    
    private void DemoDampingSection()
    {
        if (_engine == null) return;
        
        // Criar três esferas com diferentes níveis de amortecimento
        float y = 8f;
        
        // Esfera 1: Sem amortecimento (no ar)
        var sphere1 = _engine.CreatePhysicsGameObject(
            "Sphere_Undamped",
            new Vector3(-4, y, 2),
            _engine.CreateSphereShape(1f), // Esferas maiores
            Material.Metal,
            2f, // Massa maior
            BodyProperties.Undamped
        );
        var renderer1 = _engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 0f, 1f));
        sphere1.AddComponent(renderer1);
        sphere1.Tag = "DemoObject";
        
        // Esfera 2: Amortecimento padrão
        var sphere2 = _engine.CreatePhysicsGameObject(
            "Sphere_Default",
            new Vector3(0, y, 2),
            _engine.CreateSphereShape(1f),
            Material.Metal,
            2f,
            BodyProperties.Default
        );
        var renderer2 = _engine.CreateSphereRenderer(new RgbaFloat(0f, 1f, 0f, 1f));
        sphere2.AddComponent(renderer2);
        sphere2.Tag = "DemoObject";
        
        // Esfera 3: Alto amortecimento (como se estivesse em líquido)
        var sphere3 = _engine.CreatePhysicsGameObject(
            "Sphere_HighDamping",
            new Vector3(4, y, 2),
            _engine.CreateSphereShape(1f),
            Material.Metal,
            2f,
            BodyProperties.Underwater
        );
        var renderer3 = _engine.CreateSphereRenderer(new RgbaFloat(0f, 0f, 1f, 1f));
        sphere3.AddComponent(renderer3);
        sphere3.Tag = "DemoObject";
        
        // Aplicar velocidade inicial para demonstrar o amortecimento
        sphere1.GetComponent<RigidbodyComponent>()?.SetVelocity(new Vector3(0, -2, 0), Vector3.Zero);
        sphere2.GetComponent<RigidbodyComponent>()?.SetVelocity(new Vector3(0, -2, 0), Vector3.Zero);
        sphere3.GetComponent<RigidbodyComponent>()?.SetVelocity(new Vector3(0, -2, 0), Vector3.Zero);
    }
    
    private void DemoFrictionSection()
    {
        if (_engine == null) return;
        
        // Criar cubos nas rampas para demonstrar diferentes atritos
        float startY = 4f;
        
        // Cubo na rampa de gelo
        var iceCube = _engine.CreatePhysicsGameObject(
            "Cube_OnIce",
            new Vector3(-8, startY, -8),
            _engine.CreateBoxShape(new Vector3(1.5f, 1.5f, 1.5f)), // Cubos maiores
            Material.Wood,
            3f, // Massa maior
            BodyProperties.Default
        );
        var iceRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        iceCube.AddComponent(iceRenderer);
        iceCube.Tag = "DemoObject";
        
        // Cubo na rampa de madeira áspera
        var woodCube = _engine.CreatePhysicsGameObject(
            "Cube_OnWood",
            new Vector3(0, startY, -8),
            _engine.CreateBoxShape(new Vector3(1.5f, 1.5f, 1.5f)),
            Material.Wood,
            3f,
            BodyProperties.Default
        );
        var woodRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        woodCube.AddComponent(woodRenderer);
        woodCube.Tag = "DemoObject";
        
        // Cubo na rampa de metal liso
        var metalCube = _engine.CreatePhysicsGameObject(
            "Cube_OnMetal",
            new Vector3(8, startY, -8),
            _engine.CreateBoxShape(new Vector3(1.5f, 1.5f, 1.5f)),
            Material.Wood,
            3f,
            BodyProperties.Default
        );
        var metalRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        metalCube.AddComponent(metalRenderer);
        metalCube.Tag = "DemoObject";
    }
    
    private void DemoRestitutionSection()
    {
        if (_engine == null) return;
        
        // Criar bolas com diferentes níveis de restituição
        float y = 8f;
        
        // Bola 1: Vidro (baixa restituição)
        var glassBall = _engine.CreatePhysicsGameObject(
            "Ball_Glass",
            new Vector3(-6, y, 5),
            _engine.CreateSphereShape(0.4f),
            Material.Glass,
            0.5f
        );
        var glassRenderer = _engine.CreateSphereRenderer(new RgbaFloat(0.7f, 0.9f, 1f, 0.8f));
        glassBall.AddComponent(glassRenderer);
        glassBall.Tag = "DemoObject";
        
        // Bola 2: Borracha normal
        var rubberBall = _engine.CreatePhysicsGameObject(
            "Ball_Rubber",
            new Vector3(-2, y, 5),
            _engine.CreateSphereShape(0.4f),
            Material.Rubber,
            0.5f
        );
        var rubberRenderer = _engine.CreateSphereRenderer(new RgbaFloat(0.2f, 0.2f, 0.2f, 1f));
        rubberBall.AddComponent(rubberRenderer);
        rubberBall.Tag = "DemoObject";
        
        // Bola 3: Super borracha (alta restituição)
        var superBall = _engine.CreatePhysicsGameObject(
            "Ball_SuperBouncy",
            new Vector3(2, y, 5),
            _engine.CreateSphereShape(0.4f),
            Material.RubberBouncy,
            0.5f
        );
        var superRenderer = _engine.CreateSphereRenderer(new RgbaFloat(1f, 0f, 1f, 1f));
        superBall.AddComponent(superRenderer);
        superBall.Tag = "DemoObject";
        
        // Bola 4: Metal (restituição média)
        var metalBall = _engine.CreatePhysicsGameObject(
            "Ball_Metal",
            new Vector3(6, y, 5),
            _engine.CreateSphereShape(0.4f),
            Material.Metal,
            0.5f
        );
        var metalBallRenderer = _engine.CreateSphereRenderer(new RgbaFloat(0.7f, 0.7f, 0.8f, 1f));
        metalBall.AddComponent(metalBallRenderer);
        metalBall.Tag = "DemoObject";
    }
    
    private void DemoAngularMomentumSection()
    {
        if (_engine == null) return;
        
        // Criar objetos girando para demonstrar conservação de momento angular
        
        // Cubo grande com baixa velocidade angular
        var largeCube = _engine.CreatePhysicsGameObject(
            "LargeSpinningCube",
            new Vector3(-5, 5, 0),
            _engine.CreateBoxShape(new Vector3(2, 2, 2)),
            Material.Metal,
            5f,
            BodyProperties.Space // Sem amortecimento para ver conservação pura
        );
        var largeRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.8f, 0.2f, 0.2f, 1f));
        largeCube.AddComponent(largeRenderer);
        largeCube.Tag = "DemoObject";
        largeCube.GetComponent<RigidbodyComponent>()?.SetVelocity(
            Vector3.Zero, 
            new Vector3(0, 2f, 0) // Rotação em Y
        );
        
        // Cubo pequeno com alta velocidade angular
        var smallCube = _engine.CreatePhysicsGameObject(
            "SmallSpinningCube",
            new Vector3(5, 5, 0),
            _engine.CreateBoxShape(Vector3.One * 0.5f),
            Material.Metal,
            0.5f,
            BodyProperties.Space
        );
        var smallRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.2f, 0.8f, 0.2f, 1f));
        smallCube.AddComponent(smallRenderer);
        smallCube.Tag = "DemoObject";
        smallCube.GetComponent<RigidbodyComponent>()?.SetVelocity(
            Vector3.Zero,
            new Vector3(0, 8f, 0) // Rotação rápida em Y
        );
        
        // Objeto complexo girando em múltiplos eixos
        var complexObject = _engine.CreatePhysicsGameObject(
            "ComplexSpinner",
            new Vector3(0, 5, 0),
            _engine.CreateBoxShape(new Vector3(1.5f, 0.5f, 0.5f)),
            Material.Wood,
            2f,
            BodyProperties.Space
        );
        var complexRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.6f, 0.4f, 0.2f, 1f));
        complexObject.AddComponent(complexRenderer);
        complexObject.Tag = "DemoObject";
        complexObject.GetComponent<RigidbodyComponent>()?.SetVelocity(
            Vector3.Zero,
            new Vector3(3f, 5f, 2f) // Rotação em múltiplos eixos
        );
    }
    
    private void DemoSpecialPropertiesSection()
    {
        if (_engine == null) return;
        
        // Demonstrar limites de velocidade e outras propriedades especiais
        
        // Objeto com limite de velocidade (como se tivesse resistência do ar)
        var limitedObject = _engine.CreatePhysicsGameObject(
            "VelocityLimited",
            new Vector3(-5, 15, 0),
            _engine.CreateSphereShape(0.6f),
            Material.Rubber,
            1f,
            new BodyProperties(
                linearDamping: 0.1f,
                angularDamping: 0.1f,
                maxLinearVelocity: 5f,  // Velocidade máxima limitada
                maxAngularVelocity: 10f,
                gravityScale: 1f
            )
        );
        var limitedRenderer = _engine.CreateSphereRenderer(new RgbaFloat(1f, 1f, 0f, 1f));
        limitedObject.AddComponent(limitedRenderer);
        limitedObject.Tag = "DemoObject";
        
        // Objeto com gravidade reduzida (como um balão)
        var balloonObject = _engine.CreatePhysicsGameObject(
            "Balloon",
            new Vector3(0, 5, 0),
            _engine.CreateSphereShape(0.8f),
            Material.Rubber,
            0.1f,
            new BodyProperties(
                linearDamping: 0.05f,
                angularDamping: 0.05f,
                maxLinearVelocity: float.MaxValue,
                maxAngularVelocity: float.MaxValue,
                gravityScale: 0.1f  // Gravidade muito reduzida
            )
        );
        var balloonRenderer = _engine.CreateSphereRenderer(new RgbaFloat(1f, 0.5f, 0.5f, 0.8f));
        balloonObject.AddComponent(balloonRenderer);
        balloonObject.Tag = "DemoObject";
        
        // Objeto pesado com gravidade aumentada
        var heavyObject = _engine.CreatePhysicsGameObject(
            "HeavyObject",
            new Vector3(5, 5, 0),
            _engine.CreateBoxShape(Vector3.One * 1.2f),
            Material.Metal,
            10f,
            new BodyProperties(
                linearDamping: 0.01f,
                angularDamping: 0.01f,
                maxLinearVelocity: float.MaxValue,
                maxAngularVelocity: float.MaxValue,
                gravityScale: 2f  // Gravidade dobrada
            )
        );
        var heavyRenderer = _engine.CreateCubeRenderer(new RgbaFloat(0.3f, 0.3f, 0.3f, 1f));
        heavyObject.AddComponent(heavyRenderer);
        heavyObject.Tag = "DemoObject";
    }
    
    private void CleanupDynamicObjects()
    {
        if (_engine == null) return;
        
        var objectsToRemove = _engine.FindGameObjectsWithTag("DemoObject").ToList();
        foreach (var obj in objectsToRemove)
        {
            RemoveGameObject(obj);
        }
    }

    public override void Update(float deltaTime)
    {
        if (_engine == null) return;
        
        // TESTE SIMPLES - apenas verificar se a esfera saiu da área
        var testSphere = _engine.FindGameObject("TestSphere");
        if (testSphere != null)
        {
            // Log da posição a cada segundo para monitorar
            if ((int)(Time.TotalTime) % 2 == 0 && Time.TotalTime - (int)Time.TotalTime < 0.1f)
            {
                LoggingService.LogInfo($"TestSphere position: {testSphere.Position}");
            }
            
            if (testSphere.Position.Y < -10)
            {
                LoggingService.LogWarning("TestSphere fell through! Collision not working. Resetting position.");
                // Se a esfera caiu muito, reposicionar ela no topo
                var rigidbody = testSphere.GetComponent<RigidbodyComponent>();
                if (rigidbody != null)
                {
                    rigidbody.Pose = new BepuPhysics.RigidPose(new Vector3(0, 5, 0), Quaternion.Identity);
                    rigidbody.SetVelocity(Vector3.Zero, Vector3.Zero);
                }
            }
        }
        
        /* LÓGICA COMPLEXA COMENTADA POR ENQUANTO
        _sectionTimer += deltaTime;
        _timeSinceLastSpawn += deltaTime;
        
        // Mudar de seção a cada 20 segundos
        if (_sectionTimer > 20f)
        {
            _demoSection = (_demoSection + 1) % 5; // 5 seções no total
            StartDemoSection(_demoSection);
        }
        
        // Adicionar objetos extras dependendo da seção
        if (_timeSinceLastSpawn > 3f)
        {
            switch (_demoSection)
            {
                case 2: // Seção de restituição - adicionar mais bolas
                    if (_random.NextDouble() > 0.5)
                    {
                        var pos = new Vector3(
                            (float)(_random.NextDouble() * 10 - 5),
                            10,
                            5
                        );
                        var ball = _engine.CreatePhysicsGameObject(
                            $"ExtraBall_{DateTime.Now.Ticks}",
                            pos,
                            _engine.CreateSphereShape(0.3f),
                            Material.RubberBouncy,
                            0.3f
                        );
                        var renderer = _engine.CreateSphereRenderer(
                            new RgbaFloat(
                                (float)_random.NextDouble(),
                                (float)_random.NextDouble(),
                                (float)_random.NextDouble(),
                                1f
                            )
                        );
                        ball.AddComponent(renderer);
                        ball.Tag = "DemoObject";
                    }
                    break;
            }
            _timeSinceLastSpawn = 0;
        }
        
        // Remover objetos que caíram muito baixo
        var objectsToRemove = _engine.FindGameObjectsWithTag("DemoObject")
            .Where(obj => obj.Position.Y < -20)
            .ToList();
            
        foreach (var obj in objectsToRemove)
        {
            RemoveGameObject(obj);
        }
        */
    }
    
    public override void Shutdown()
    {
        // Desativar física avançada ao sair
        _engine?.DisableAdvancedPhysics();
    }
} 