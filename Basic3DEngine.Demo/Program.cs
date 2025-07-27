using Basic3DEngine.Core;

namespace Basic3DEngine.Demo;

internal class Program
{
    private static void Main(string[] args)
    {
        // Cria a engine
        var engine = new Engine();
        
        // Criar classe wrapper que chama o DemoGame no Update
        var gameWrapper = new DemoGameWrapper(engine);
        
        // Executa o jogo usando a engine
        engine.Run(gameWrapper);
    }
}

/// <summary>
/// Wrapper para conectar DemoGame ao sistema Game da engine
/// </summary>
public class DemoGameWrapper : Game
{
    private readonly DemoGame _demoGame;
    
    public DemoGameWrapper(Engine engine)
    {
        _demoGame = new DemoGame(engine);
    }
    
    public override void Initialize(Engine engine)
    {
        // DemoGame agora se inicializa no próprio Update quando a física estiver pronta
    }
    
    public override void Update(float deltaTime)
    {
        _demoGame.Update(deltaTime);
    }
}