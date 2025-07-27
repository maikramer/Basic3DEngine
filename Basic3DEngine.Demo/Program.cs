namespace Basic3DEngine.Demo;

internal class Program
{
    private static void Main(string[] args)
    {
        // Cria a engine
        var engine = new Engine();
        
        // Cria o jogo
        var game = new DemoGame();
        
        // Executa o jogo usando a engine
        engine.Run(game);
    }
}