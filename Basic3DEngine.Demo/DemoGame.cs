using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Rendering;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Demo;

public class DemoGame
{
    private readonly Engine _engine;
    private float _demoTimer = 0f;
    private bool _initialized = false;
    private float _nextObjectSpawnTime = 0f;
    private readonly Random _random = new();
    private bool _useLighting = true; // Usar novo sistema de contraste - tecla L alterna para comparar

    public DemoGame(Engine engine)
    {
        _engine = engine;
    }
    
    public void Update(float deltaTime)
    {
        // Inicializar na primeira execução quando a física estiver disponível
        if (!_initialized && _engine.PhysicsWorld != null)
        {
            LoggingService.LogInfo("DemoGame.Update called - physics is ready");
            LoggingService.LogInfo("Gravity set to: <0. -9,81. 0>");
            _engine.SetGravity(9.81f);
            
                    LoggingService.LogInfo($"Creating simple scene... Lighting mode: {(_useLighting ? "LIT" : "UNLIT")}");
        LoggingService.LogInfo("=== CONTROLS ===");
        LoggingService.LogInfo("WASD: Move camera");
        LoggingService.LogInfo("Space/LShift: Up/Down");
        LoggingService.LogInfo("Mouse: Look around (when captured)");
        LoggingService.LogInfo("F1: Toggle mouse capture");
        LoggingService.LogInfo("L: Toggle lighting mode");
        LoggingService.LogInfo("Left Click: Spawn sphere");
        LoggingService.LogInfo("Right Click: Spawn cube");
        LoggingService.LogInfo("ESC: Exit");
        LoggingService.LogInfo("================");
            
            // Criar cenário simples usando métodos intuitivos da engine
            CreateSimpleScene();
            
            LoggingService.LogInfo("Simple scene created successfully");
            
            _initialized = true;
        }
        
        if (!_initialized) 
        {
            LoggingService.LogInfo("DemoGame.Update called but physics not ready yet");
            return; // Aguardar inicialização
        }
        
        _demoTimer += deltaTime;
        
        // Input interativo
        HandleInteractiveInput();
        
        // Spawn automático de objetos ocasionalmente
        HandleAutoSpawn();
    }
    
    private void CreateSimpleScene()
    {
        CreateRollingScene();
    }
    
    private void CreateRollingScene()
    {
        // Criar skybox procedural primeiro
        _engine.CreateSkybox();
        LoggingService.LogInfo("Procedural skybox added to scene");
        
        // Cores da demo
        var groundColor = new RgbaFloat(0.7f, 0.7f, 0.7f, 1f);
        var rampColor = new RgbaFloat(0.8f, 0.6f, 0.4f, 1f);
        var sphereColor1 = new RgbaFloat(1f, 0f, 0f, 1f); // Vermelho
        var sphereColor2 = new RgbaFloat(0f, 0f, 1f, 1f); // Azul
        var sphereColor3 = new RgbaFloat(0f, 1f, 0f, 1f); // Verde
        
        // Chão principal - TEMPORARIAMENTE SEM ILUMINAÇÃO PARA DEBUG
        var groundThickness = 1f;
        var groundPosition = new Vector3(0, -groundThickness * 0.5f, 0);
        var groundSize = new Vector3(30f, groundThickness, 20f);
        
        // TESTE: Chão com iluminação para comparar
        _engine.CreateGroundLit(groundPosition, groundSize, groundColor, 8f, 0.1f);
        LoggingService.LogInfo($"Ground LIT created at {groundPosition} with size {groundSize}");
        
        // Rampa inclinada
        var rampPosition = new Vector3(-6f, 3f, 0f);
        var rampSize = new Vector3(12f, 0.5f, 6f);
        var rampAngle = -15f;
        
        // Rampa com iluminação - brilhante
        var rampObject = _engine.CreateStaticCubeLit("Ramp", rampPosition, rampSize, rampColor, 64f, 0.4f);
        // Aplicar rotação manualmente
        var rampRigidbody = rampObject.GetComponent<RigidbodyComponent>();
        if (rampRigidbody != null)
        {
            var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(0, 0, MathF.PI * rampAngle / 180f);
            rampRigidbody.Pose = new BepuPhysics.RigidPose(rampPosition, rotationQuaternion);
        }
        LoggingService.LogInfo($"Ramp LIT created with rotation {rampAngle}°");
        
        // Parede
        var wallPosition = new Vector3(15f, 2f, 0f);
        var wallSize = new Vector3(1f, 4f, 15f);
        
        // Parede com iluminação - semi-brilhante
        _engine.CreateStaticCubeLit("Wall", wallPosition, wallSize, groundColor, 16f, 0.2f);
        LoggingService.LogInfo($"Wall LIT created at {wallPosition} with size {wallSize}");
        
        // TESTE: Esferas exatamente no centro da rampa para debug
        // Rampa está em X=-6, Z=0, então vamos posicionar as esferas bem no centro
        _engine.CreateSphere("RollingSphere1", new Vector3(-10f, 8f, 0f), 0.5f, sphereColor1, 2f);
        _engine.CreateSphere("RollingSphere2", new Vector3(-8f, 8f, 0f), 0.7f, sphereColor2, 3f);
        _engine.CreateSphere("RollingSphere3", new Vector3(-6f, 8f, 0f), 0.4f, sphereColor3, 1.5f);
        
        LoggingService.LogInfo("Rolling scene created - esferas irão rolar pela rampa!");
    }
    
    private void HandleInteractiveInput()
    {
        // Criar objetos com o mouse
        if (InputService.IsMouseButtonPressed(MouseButton.Left))
        {
            SpawnObjectAtCameraDirection(isBox: false); // Esfera
        }
        
        if (InputService.IsMouseButtonPressed(MouseButton.Right))
        {
            SpawnObjectAtCameraDirection(isBox: true); // Cubo
        }
        
        // Reset da cena com R
        if (InputService.IsKeyPressed(Key.R))
        {
            ResetScene();
        }
        
        // Informações de controle com F1
        if (InputService.IsKeyPressed(Key.F1))
        {
            ShowControls();
        }
        
        // Mudar velocidade da câmera com +/-
        if (InputService.IsKeyPressed(Key.Plus) || InputService.IsKeyPressed(Key.KeypadPlus))
        {
            _engine.Camera.MovementSpeed += 2f;
            LoggingService.LogInfo($"Camera speed: {_engine.Camera.MovementSpeed:F1}");
        }
        
        if (InputService.IsKeyPressed(Key.Minus) || InputService.IsKeyPressed(Key.KeypadMinus))
        {
            _engine.Camera.MovementSpeed = MathF.Max(1f, _engine.Camera.MovementSpeed - 2f);
            LoggingService.LogInfo($"Camera speed: {_engine.Camera.MovementSpeed:F1}");
        }
        
        // Alternar iluminação com L
        if (InputService.IsKeyPressed(Key.L))
        {
            _useLighting = !_useLighting;
            LoggingService.LogInfo($"Lighting mode: {(_useLighting ? "Advanced (Lit)" : "Basic (Unlit)")}");
        }
        
        // Controles de iluminação com 1, 2, 3
        if (InputService.IsKeyPressed(Key.Number1))
        {
            _engine.Lighting.SetupDefaultLighting();
            LoggingService.LogInfo("Switched to default lighting");
        }
        
        if (InputService.IsKeyPressed(Key.Number2))
        {
            _engine.Lighting.SetupNightLighting();
            LoggingService.LogInfo("Switched to night lighting");
        }
        
        if (InputService.IsKeyPressed(Key.Number3))
        {
            // Iluminação personalizada dramática
            _engine.Lighting.ClearLights();
            
            // Luz forte vermelha de um lado
            var redLight = LightData.CreateDirectional(
                new Vector3(1f, -0.5f, 0f),
                new Vector3(1f, 0.2f, 0.2f),
                0.7f
            );
            _engine.Lighting.AddLight(redLight);
            
            // Luz azul do outro lado
            var blueLight = LightData.CreateDirectional(
                new Vector3(-1f, -0.3f, 0f),
                new Vector3(0.2f, 0.4f, 1f),
                0.5f
            );
            _engine.Lighting.AddLight(blueLight);
            
            _engine.Lighting.AmbientColor = new Vector3(0.1f, 0.05f, 0.15f);
            _engine.Lighting.AmbientIntensity = 0.2f;
            
            LoggingService.LogInfo("Switched to dramatic lighting");
        }
    }
    
    private void HandleAutoSpawn()
    {
        // Spawn automático a cada 8 segundos
        if (_demoTimer >= _nextObjectSpawnTime)
        {
            _nextObjectSpawnTime = _demoTimer + 8f;
            
            // Criar uma esfera colorida aleatória na parte alta da rampa
            var colors = new[]
            {
                new RgbaFloat(1f, 0.5f, 0f, 1f), // Laranja
                new RgbaFloat(1f, 0f, 1f, 1f),   // Magenta 
                new RgbaFloat(0f, 1f, 1f, 1f),   // Ciano
                new RgbaFloat(1f, 1f, 0f, 1f),   // Amarelo
                new RgbaFloat(0.5f, 0f, 1f, 1f), // Roxo
            };
            
            var randomColor = colors[_random.Next(colors.Length)];
            var randomX = -12f + (float)_random.NextDouble() * 4f; // Varia entre -12 e -8
            var randomRadius = 0.3f + (float)_random.NextDouble() * 0.4f; // Raio entre 0.3 e 0.7
            var randomMass = 1f + (float)_random.NextDouble() * 3f; // Massa entre 1 e 4
            
            var autoSphere = $"AutoSphere_{_demoTimer:F0}";
            _engine.CreateSphere(autoSphere, new Vector3(randomX, 10f, 0f), randomRadius, randomColor, randomMass);
            
            LoggingService.LogInfo($"Auto-spawned sphere: {autoSphere} (mass: {randomMass:F1}, radius: {randomRadius:F1})");
        }
    }
    
    private void SpawnObjectAtCameraDirection(bool isBox)
    {
        var camera = _engine.Camera;
        var spawnPos = camera.Position + camera.Forward * 3f; // 3 metros à frente da câmera
        
        var colors = new[]
        {
            new RgbaFloat(1f, 0f, 0f, 1f), // Vermelho
            new RgbaFloat(0f, 1f, 0f, 1f), // Verde
            new RgbaFloat(0f, 0f, 1f, 1f), // Azul
            new RgbaFloat(1f, 1f, 0f, 1f), // Amarelo
            new RgbaFloat(1f, 0f, 1f, 1f), // Magenta
            new RgbaFloat(0f, 1f, 1f, 1f), // Ciano
        };
        
        var randomColor = colors[_random.Next(colors.Length)];
        var objectName = $"{(isBox ? "UserCube" : "UserSphere")}_{_demoTimer:F1}";
        
        GameObject newObject;
        if (isBox)
        {
            var size = new Vector3(0.5f + (float)_random.NextDouble() * 0.5f); // Tamanho entre 0.5 e 1.0
            
            if (_useLighting)
            {
                // Criar cubo com iluminação e propriedades variadas
                var shininess = 16f + (float)_random.NextDouble() * 48f; // 16-64
                var specular = 0.2f + (float)_random.NextDouble() * 0.5f; // 0.2-0.7
                newObject = _engine.CreateCubeLit(objectName, spawnPos, size, randomColor, 2f, shininess, specular);
            }
            else
            {
                newObject = _engine.CreateCube(objectName, spawnPos, size, randomColor, 2f);
            }
        }
        else
        {
            var radius = 0.2f + (float)_random.NextDouble() * 0.3f; // Raio entre 0.2 e 0.5
            // Por enquanto, esferas ainda usam o renderer original
            newObject = _engine.CreateSphere(objectName, spawnPos, radius, randomColor, 1.5f);
        }
        
        // Adicionar um impulso na direção da câmera
        var rigidbody = newObject.GetComponent<RigidbodyComponent>();
        if (rigidbody != null)
        {
            var forceDirection = camera.Forward;
            var forceMagnitude = 5f + (float)_random.NextDouble() * 5f; // Entre 5 e 10
            rigidbody.AddImpulse(forceDirection * forceMagnitude);
        }
        
        LoggingService.LogInfo($"Spawned {objectName} at camera position with impulse");
    }
    
    private void ResetScene()
    {
        LoggingService.LogInfo("Resetting scene...");
        
        // TODO: Implementar remoção de objetos específicos
        // Por enquanto, apenas log
        LoggingService.LogInfo("Scene reset requested - funcionalidade será implementada");
    }
    
    private void ShowControls()
    {
        LoggingService.LogInfo("=== CONTROLES DA DEMO ===");
        LoggingService.LogInfo("WASD: Mover câmera");
        LoggingService.LogInfo("Mouse: Rotacionar câmera");
        LoggingService.LogInfo("Space/Shift: Subir/Descer");
        LoggingService.LogInfo("Scroll: Mudar velocidade");
        LoggingService.LogInfo("Click Esquerdo: Criar esfera");
        LoggingService.LogInfo("Click Direito: Criar cubo");
        LoggingService.LogInfo("R: Resetar cena");
        LoggingService.LogInfo("F1: Mostrar controles");
        LoggingService.LogInfo("L: Alternar iluminação (Lit/Unlit)");
        LoggingService.LogInfo("1: Iluminação padrão (sol)");
        LoggingService.LogInfo("2: Iluminação noturna");
        LoggingService.LogInfo("3: Iluminação dramática");
        LoggingService.LogInfo("ESC: Sair");
        LoggingService.LogInfo("========================");
    }
} 