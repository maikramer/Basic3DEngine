using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Physics.Shapes;
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
    
    // Time management
    private float _fixedTimeAccumulator;

    /// <summary>
    /// Mundo físico da engine
    /// </summary>
    public PhysicsWorldBepu? PhysicsWorld => _physicsWorldBepu;

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
            
            // Atualizar Time
            Time.UnscaledDeltaTime = deltaTime;
            Time.DeltaTime = deltaTime * Time.TimeScale;
            Time.TotalTime += Time.DeltaTime;

            // Física com fixed timestep
            _fixedTimeAccumulator += Time.DeltaTime;
            while (_fixedTimeAccumulator >= Time.FixedDeltaTime)
            {
                // Atualizar física
                _physicsWorldBepu?.Update(Time.FixedDeltaTime);
                _fixedTimeAccumulator -= Time.FixedDeltaTime;
            }

            // Atualizar GameObjects
            foreach (var gameObject in _gameObjects) 
                gameObject?.Update(Time.DeltaTime);
                
            // Atualizar o jogo
            _game.Update(Time.DeltaTime);

            // Processar eventos da janela
            _window.PumpEvents();

            // Atualizar câmera
            _cameraRotation += Time.DeltaTime * 0.2f;
            _cameraPosition = Vector3.Transform(new Vector3(0, 3, 7),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, _cameraRotation));

            // Renderizar
            Render();
            
            // Log periódico a cada 60 frames
            if (frameCount % 60 == 0)
            {
                LoggingService.LogInfo($"Frame {frameCount} - FPS: {1f / Time.UnscaledDeltaTime:F1}");
            }
        }

        LoggingService.LogInfo($"Exiting game loop after {frameCount} frames");
        
        // Shutdown do jogo
        _game.Shutdown();
        
        // Limpar recursos
        _cl?.Dispose();
        _gd?.Dispose();
        _physicsWorldBepu?.Dispose();
        
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
    /// Busca um GameObject pelo nome
    /// </summary>
    public GameObject? FindGameObject(string name)
    {
        return _gameObjects.FirstOrDefault(go => go.Name == name);
    }
    
    /// <summary>
    /// Busca todos os GameObjects com uma tag específica
    /// </summary>
    public IEnumerable<GameObject> FindGameObjectsWithTag(string tag)
    {
        return _gameObjects.Where(go => go.Tag == tag);
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
    /// Cria uma forma de caixa
    /// </summary>
    public BoxShape CreateBoxShape(Vector3 size)
    {
        return new BoxShape(size);
    }
    
    /// <summary>
    /// Cria uma forma de esfera
    /// </summary>
    public SphereShape CreateSphereShape(float radius)
    {
        return new SphereShape(radius);
    }
    
    /// <summary>
    /// Adiciona física de caixa a um rigidbody (método de compatibilidade)
    /// </summary>
    public void AddBoxPhysics(RigidbodyComponent rigidbody, Vector3 size)
    {
        rigidbody.Shape = CreateBoxShape(size);
        _physicsWorldBepu?.AddBody(rigidbody);
    }
    
    /// <summary>
    /// Adiciona física de esfera a um rigidbody (método de compatibilidade)
    /// </summary>
    public void AddSpherePhysics(RigidbodyComponent rigidbody, float radius)
    {
        rigidbody.Shape = CreateSphereShape(radius);
        _physicsWorldBepu?.AddBody(rigidbody);
    }
    
    /// <summary>
    /// Faz um raycast no mundo físico
    /// </summary>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        if (_physicsWorldBepu == null)
        {
            hit = default;
            return false;
        }
        
        return _physicsWorldBepu.Raycast(origin, direction, maxDistance, out hit);
    }
    
    // Métodos de Física Avançada
    
    /// <summary>
    /// Ativa o modo de física avançada com propriedades por corpo
    /// </summary>
    public void EnableAdvancedPhysics()
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        _physicsWorldBepu.SetPhysicsMode(PhysicsMode.Advanced);
        LoggingService.LogInfo("Advanced physics mode enabled");
    }
    
    /// <summary>
    /// Desativa o modo de física avançada, voltando ao modo simples
    /// </summary>
    public void DisableAdvancedPhysics()
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        _physicsWorldBepu.SetPhysicsMode(PhysicsMode.Simple);
        LoggingService.LogInfo("Simple physics mode enabled");
    }
    
    /// <summary>
    /// Verifica se a física avançada está ativa
    /// </summary>
    public bool IsAdvancedPhysicsEnabled => _physicsWorldBepu?.Mode == PhysicsMode.Advanced;
    
    /// <summary>
    /// Cria um rigidbody com propriedades físicas avançadas
    /// </summary>
    public RigidbodyComponent CreateAdvancedRigidbody(
        float mass = 1f,
        bool isStatic = false,
        float linearDamping = 0.03f,
        float angularDamping = 0.03f,
        float gravityScale = 1f,
        float maxLinearVelocity = float.MaxValue,
        float maxAngularVelocity = float.MaxValue)
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        var rigidbody = new RigidbodyComponent(_physicsWorldBepu, mass, isStatic);
        
        // Configurar propriedades avançadas
        rigidbody.BodyProperties = new BodyProperties(
            linearDamping,
            angularDamping,
            maxLinearVelocity,
            maxAngularVelocity,
            gravityScale
        );
        
        return rigidbody;
    }
    
    /// <summary>
    /// Cria um rigidbody com propriedades pré-definidas
    /// </summary>
    public RigidbodyComponent CreateRigidbodyWithPreset(
        float mass = 1f,
        bool isStatic = false,
        BodyProperties preset = default)
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        var rigidbody = new RigidbodyComponent(_physicsWorldBepu, mass, isStatic);
        
        // Usar preset se fornecido, senão usar Default
        rigidbody.BodyProperties = preset.Equals(default(BodyProperties)) 
            ? BodyProperties.Default 
            : preset;
        
        return rigidbody;
    }
    
    /// <summary>
    /// Define o amortecimento global padrão
    /// </summary>
    public void SetGlobalDamping(float linearDamping, float angularDamping)
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        _physicsWorldBepu.SetDefaultDamping(linearDamping, angularDamping);
        LoggingService.LogInfo($"Global damping set to: Linear={linearDamping}, Angular={angularDamping}");
    }
    
    /// <summary>
    /// Define a gravidade global
    /// </summary>
    public void SetGravity(Vector3 gravity)
    {
        if (_physicsWorldBepu == null)
            throw new InvalidOperationException("Physics not initialized");
            
        _physicsWorldBepu.SetDefaultGravity(gravity);
        LoggingService.LogInfo($"Gravity set to: {gravity}");
    }
    
    /// <summary>
    /// Define a gravidade global usando um valor escalar (Y negativo)
    /// </summary>
    public void SetGravity(float gravity)
    {
        SetGravity(new Vector3(0, -Math.Abs(gravity), 0));
    }
    
    /// <summary>
    /// Aplica propriedades físicas a um rigidbody existente
    /// </summary>
    public void ApplyBodyProperties(RigidbodyComponent rigidbody, BodyProperties properties)
    {
        if (rigidbody == null)
            throw new ArgumentNullException(nameof(rigidbody));
            
        rigidbody.BodyProperties = properties;
    }
    
    /// <summary>
    /// Cria um GameObject com física avançada
    /// </summary>
    public GameObject CreatePhysicsGameObject(
        string name,
        Vector3 position,
        IPhysicsShape shape,
        Material material,
        float mass = 1f,
        BodyProperties? bodyProperties = null)
    {
        var gameObject = new GameObject(name)
        {
            Position = position
        };
        
        // Criar rigidbody
        var rigidbody = bodyProperties.HasValue 
            ? CreateRigidbodyWithPreset(mass, false, bodyProperties.Value)
            : CreateRigidbody(mass, false);
            
        rigidbody.Shape = shape;
        rigidbody.Material = material;
        
        // IMPORTANTE: Definir a pose ANTES de adicionar ao mundo físico
        rigidbody.Pose = new BepuPhysics.RigidPose(position, Quaternion.Identity);
        
        gameObject.AddComponent(rigidbody);
        
        // Adicionar ao mundo físico
        _physicsWorldBepu?.AddBody(rigidbody);
        
        // Adicionar à cena
        AddGameObject(gameObject);
        
        return gameObject;
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