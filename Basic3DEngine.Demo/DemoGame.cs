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
        // Chão simples com superfície em Y = 0
        _engine.CreateGroundAtLevel(
            groundLevel: 0f,    // Superfície do chão em Y = 0
            thickness: 1f,      // Espessura de 1 unidade
            width: 20f,         // Largura
            depth: 20f          // Profundidade
        );
        
        // Teste com duas esferas de raios diferentes para depuração
        _engine.CreateSphere(
            "TestSphere1",                            // Nome
            new Vector3(-2, 5, 0),                   // Posição inicial
            0.5f,                                     // Raio 0.5
            new RgbaFloat(1f, 0f, 0f, 1f),           // Vermelho
            2f                                        // Massa
        );
        
        _engine.CreateSphere(
            "TestSphere2",                            // Nome
            new Vector3(2, 5, 0),                    // Posição inicial
            1.0f,                                     // Raio 1.0
            new RgbaFloat(0f, 0f, 1f, 1f),           // Azul
            2f                                        // Massa
        );
    }
} 