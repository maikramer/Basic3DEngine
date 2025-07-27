using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Services;
using Veldrid;

namespace Basic3DEngine.Demo;

public class DemoGame
{
    private readonly Engine _engine;
    private float _demoTimer = 0f;
    private bool _initialized = false;

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
            
            LoggingService.LogInfo("Creating simple scene...");
            
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
        
        // Aqui você pode adicionar lógica adicional do jogo conforme necessário
    }
    
    private void CreateSimpleScene()
    {
        CreateRollingScene();
    }
    
    private void CreateRollingScene()
    {
        // Cores da demo
        var groundColor = new RgbaFloat(0.7f, 0.7f, 0.7f, 1f);
        var rampColor = new RgbaFloat(0.8f, 0.6f, 0.4f, 1f);
        var sphereColor1 = new RgbaFloat(1f, 0f, 0f, 1f); // Vermelho
        var sphereColor2 = new RgbaFloat(0f, 0f, 1f, 1f); // Azul
        var sphereColor3 = new RgbaFloat(0f, 1f, 0f, 1f); // Verde
        
        // Chão principal - posicionar para que a superfície fique em Y = 0
        var groundThickness = 1f;
        var groundPosition = new Vector3(0, -groundThickness * 0.5f, 0);
        var groundSize = new Vector3(30f, groundThickness, 20f);
        _engine.CreateStaticCube("Ground", groundPosition, groundSize, groundColor);
        
        // Rampa inclinada maior para as esferas rolarem (inclinada para baixo no sentido direito)
        var rampPosition = new Vector3(-6f, 3f, 0f);
        var rampSize = new Vector3(12f, 0.5f, 6f); // Maior: 12x6 em vez de 8x4
        var rampAngle = -15f; // -15 graus = inclinada para baixo no lado direito
        _engine.CreateRamp("Ramp", rampPosition, rampSize, rampAngle, rampColor);
        
        // Parede no final para as esferas não saírem
        var wallPosition = new Vector3(15f, 2f, 0f);
        var wallSize = new Vector3(1f, 4f, 15f);
        _engine.CreateStaticCube("Wall", wallPosition, wallSize, groundColor);
        
        // TESTE: Esferas exatamente no centro da rampa para debug
        // Rampa está em X=-6, Z=0, então vamos posicionar as esferas bem no centro
        _engine.CreateSphere("RollingSphere1", new Vector3(-10f, 8f, 0f), 0.5f, sphereColor1, 2f);
        _engine.CreateSphere("RollingSphere2", new Vector3(-8f, 8f, 0f), 0.7f, sphereColor2, 3f);
        _engine.CreateSphere("RollingSphere3", new Vector3(-6f, 8f, 0f), 0.4f, sphereColor3, 1.5f);
        
        LoggingService.LogInfo("Rolling scene created - esferas irão rolar pela rampa!");
    }
} 