using System.Numerics;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Services;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Basic3DEngine;

public class Engine
{
    // Camera
    private Vector3 _cameraPosition = new(0, 3, 7);
    private float _cameraRotation;
    private readonly Vector3 _cameraTarget = new(0, 0, 0);
    private CommandList? _cl;
    private ResourceFactory? _factory;

    // GameObjects
    private readonly List<GameObject> _gameObjects = new();
    private GraphicsDevice? _gd;

    // Physics
    private PhysicsWorldBepu? _physicsWorldBepu;
    
    // Game instance
    private Game? _game;
    
    // Window
    private Sdl2Window? _window;

    /// <summary>
    /// Executa um jogo usando a engine
    /// </summary>
    public void Run(Game game)
    {
        _game = game;
        
        // Inicializar o serviço de logging
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "engine.log");
        LoggingService.Initialize(logPath);
        LoggingService.LogInfo($"Starting Basic3DEngine with game: {game.GetType().Name}");

        // Configuração da janela
        var windowCi = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 1280,
            WindowHeight = 720,
            WindowTitle = "Basic 3D Engine"
        };

        var options = new GraphicsDeviceOptions(
            false,
            syncToVerticalBlank: true,
            swapchainDepthFormat: PixelFormat.D24_UNorm_S8_UInt
        );
        
        VeldridStartup.CreateWindowAndGraphicsDevice(windowCi, options, out _window, out _gd);
        _factory = _gd.ResourceFactory;
        _cl = _factory.CreateCommandList();

        LoggingService.LogInfo($"Graphics device created: {_gd.BackendType}");
        LoggingService.LogInfo("Command list created");

        // Inicializar física
        _physicsWorldBepu = new PhysicsWorldBepu();
        LoggingService.LogInfo("Physics world initialized");

        // Inicializar o jogo
        _game.Initialize(this);
        LoggingService.LogInfo("Game initialized");

        // Loop principal
        LoggingService.LogInfo("Entering main game loop");
        var previousTime = DateTime.Now.TimeOfDay.TotalSeconds;
        var frameCount = 0;
        
        while (_window.Exists)
        {
            frameCount++;
            var currentTime = DateTime.Now.TimeOfDay.TotalSeconds;
            var deltaTime = (float)(currentTime - previousTime);
            previousTime = currentTime;

            // Atualizar física
            _physicsWorldBepu?.Update(deltaTime);

            // Atualizar GameObjects
            foreach (var gameObject in _gameObjects) 
                gameObject?.Update(deltaTime);
                
            // Atualizar o jogo
            _game.Update(deltaTime);

            // Processar eventos da janela
            _window.PumpEvents();

            // Atualizar câmera
            _cameraRotation += deltaTime * 0.2f;
            _cameraPosition = Vector3.Transform(new Vector3(0, 3, 7),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, _cameraRotation));

            // Renderizar
            Render();
            
            // Log periódico a cada 60 frames
            if (frameCount % 60 == 0)
            {
                LoggingService.LogInfo($"Frame {frameCount} - FPS: {60.0f / (currentTime - previousTime + deltaTime * 60):F1}");
            }
        }

        LoggingService.LogInfo($"Exiting game loop after {frameCount} frames");
        
        // Shutdown do jogo
        _game.Shutdown();
        
        // Limpar recursos
        _cl?.Dispose();
        _gd?.Dispose();
        
        LoggingService.LogInfo("Resources disposed - Engine shutdown complete");
    }

    /// <summary>
    /// Adiciona um GameObject à cena
    /// </summary>
    public void AddGameObject(GameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }
    
    /// <summary>
    /// Remove um GameObject da cena
    /// </summary>
    public void RemoveGameObject(GameObject gameObject)
    {
        _gameObjects.Remove(gameObject);
    }
    
    /// <summary>
    /// Cria um componente de renderização de cubo
    /// </summary>
    public CubeRenderComponent CreateCubeRenderer(RgbaFloat color)
    {
        if (_gd == null || _factory == null)
            throw new InvalidOperationException("Engine not initialized");
            
        return new CubeRenderComponent(_gd, _factory, _factory.CreateCommandList(), color);
    }
    
    /// <summary>
    /// Cria um componente de renderização de esfera
    /// </summary>
    public SphereRenderComponent CreateSphereRenderer(RgbaFloat color, int resolution = 16)
    {
        if (_gd == null || _factory == null)
            throw new InvalidOperationException("Engine not initialized");
            
        return new SphereRenderComponent(_gd, _factory, _factory.CreateCommandList(), color, resolution);
    }
    
    /// <summary>
    /// Cria um componente de física para rigidbody
    /// </summary>
    public RigidbodyComponent CreateRigidbody(float mass = 1f, bool isStatic = false)
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        return new RigidbodyComponent(_physicsWorldBepu, mass, isStatic);
    }
    
    /// <summary>
    /// Adiciona física de caixa a um rigidbody
    /// </summary>
    public void AddBoxPhysics(RigidbodyComponent rigidbody, Vector3 size)
    {
        _physicsWorldBepu?.AddBox(rigidbody, size);
    }
    
    /// <summary>
    /// Adiciona física de esfera a um rigidbody
    /// </summary>
    public void AddSpherePhysics(RigidbodyComponent rigidbody, float radius)
    {
        _physicsWorldBepu?.AddSphere(rigidbody, radius);
    }

    private void Render()
    {
        if (_gd == null || _cl == null || _physicsWorldBepu == null)
            return;

        // Criar matrizes de visualização e projeção
        var viewMatrix = Matrix4x4.CreateLookAt(_cameraPosition, _cameraTarget, Vector3.UnitY);
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            (float)Math.PI / 3f,
            (float)_gd.MainSwapchain.Framebuffer.Width / _gd.MainSwapchain.Framebuffer.Height,
            0.1f, 1000f);

        // Começar o comando de renderização
        _cl.Begin();
        _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
        _cl.ClearColorTarget(0, RgbaFloat.Black);
        _cl.ClearDepthStencil(1f);

        // Renderizar todos os GameObjects
        foreach (var gameObject in _gameObjects)
        {
            LoggingService.LogDebug($"GameObject {gameObject.Name} - Components: {string.Join(", ", gameObject.GetAllComponents().Select(c => c.GetType().Name))}");
            var renderComponent = gameObject.GetComponent<RenderComponent>();
            if (renderComponent != null)
            {
                renderComponent.Render(_cl, viewMatrix, projectionMatrix);
            }
            else
            {
                LoggingService.LogWarning($"GameObject {gameObject.Name} has no RenderComponent");
            }
        }

        // Finalizar e executar os comandos
        _cl.End();
        _gd.SubmitCommands(_cl);
        _gd.SwapBuffers(_gd.MainSwapchain);
    }
}