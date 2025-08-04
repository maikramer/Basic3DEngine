using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Core.Interfaces;
using Basic3DEngine.Entities;
using Basic3DEngine.Physics;
using Basic3DEngine.Physics.Shapes;
using Basic3DEngine.Rendering;
using Basic3DEngine.Services;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Basic3DEngine;

public class Engine
{
    // Camera controlável
    private Camera _camera = new(new Vector3(0, 8, 15), 0f, -0.3f); // Posição inicial + ligeira inclinação para baixo
    private CommandList? _cl;
    private ResourceFactory? _factory;

    // GameObjects
    private readonly List<GameObject> _gameObjects = new();
    private GraphicsDevice? _gd;

    // Physics
    private PhysicsWorldBepu? _physicsWorldBepu;
    
    // Lighting
    private LightingSystem _lightingSystem = new();
    
    // Game instance
    private Game? _game;
    
    // Window
    private Sdl2Window? _window;
    
    // Render frame counter
    private int _renderFrameCount = 0;
    
    // Time management
    private float _fixedTimeAccumulator;

    /// <summary>
    /// Mundo físico da engine
    /// </summary>
    public PhysicsWorldBepu? PhysicsWorld => _physicsWorldBepu;
    
    /// <summary>
    /// Câmera controlável da engine
    /// </summary>
    public Camera Camera => _camera;
    
    /// <summary>
    /// Sistema de iluminação da engine
    /// </summary>
    public LightingSystem Lighting => _lightingSystem;

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

        // Inicializar iluminação padrão
        _lightingSystem.SetupDefaultLighting();
        LoggingService.LogInfo("Lighting system initialized");
        
        // Configurar eventos da janela
        _window.Resized += () => {
            LoggingService.LogInfo($"Window resized to {_window.Width}x{_window.Height}");
            OnWindowResized();
        };
        
        // Ativar mouse capture automaticamente
        InputService.SetMouseCaptured(true);

        // Inicializar o jogo
        _game.Initialize(this);
        LoggingService.LogInfo("Game initialized");

        // Loop principal
        LoggingService.LogInfo("Entering main game loop");
        var previousTime = DateTime.Now.TimeOfDay.TotalSeconds;
        var frameCount = 0;
        
        while (_window.Exists)
        {
            try
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
                    _physicsWorldBepu?.Step(Time.FixedDeltaTime);
                    _fixedTimeAccumulator -= Time.FixedDeltaTime;
                }

                // Atualizar GameObjects
                foreach (var gameObject in _gameObjects) 
                    gameObject?.Update(Time.DeltaTime);
                    
                // Atualizar o jogo
                _game.Update(Time.DeltaTime);

                // Processar eventos da janela e input
                var inputSnapshot = _window.PumpEvents();
                InputService.Update(inputSnapshot);

                // Atualizar câmera controlável
                _camera.Update(Time.DeltaTime);
                
                // Controles de mouse capture
                if (InputService.IsKeyPressed(Key.F1))
                {
                    InputService.SetMouseCaptured(!InputService.IsMouseCaptured);
                }
                
                // Verificar se o usuário quer sair
                if (InputService.IsExitRequested())
                {
                    LoggingService.LogInfo("Exit requested by user");
                    break;
                }

                // Renderizar
                Render();
                
                // Log periódico a cada 60 frames
                if (frameCount % 60 == 0)
                {
                    LoggingService.LogInfo($"Frame {frameCount} - FPS: {1f / Time.UnscaledDeltaTime:F1}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Exception in main loop frame {frameCount}: {ex.Message}");
                LoggingService.LogError($"Stack trace: {ex.StackTrace}");
                break; // Sair do loop se houver exceção
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
    /// Executa a engine sem um jogo específico (para inicialização manual)
    /// </summary>
    public void Run()
    {
        // Inicializar o serviço de logging
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "engine.log");
        LoggingService.Initialize(logPath);
        LoggingService.LogInfo("Starting Basic3DEngine");

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

        // Inicializar iluminação padrão
        _lightingSystem.SetupDefaultLighting();
        LoggingService.LogInfo("Lighting system initialized");

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
                _physicsWorldBepu?.Step(Time.FixedDeltaTime);
                _fixedTimeAccumulator -= Time.FixedDeltaTime;
            }

            // Atualizar GameObjects
            foreach (var gameObject in _gameObjects) 
                gameObject?.Update(Time.DeltaTime);

            // Processar eventos da janela e input
            var inputSnapshot = _window.PumpEvents();
            InputService.Update(inputSnapshot);

            // Atualizar câmera controlável
            _camera.Update(Time.DeltaTime);
            
            // Verificar se o usuário quer sair
            if (InputService.IsExitRequested())
            {
                LoggingService.LogInfo("Exit requested by user");
                break;
            }
            
            // Renderizar
            Render();
            
            // Controlar FPS se necessário
            if (frameCount % 300 == 0) // A cada 300 frames (cerca de 5s a 60 FPS)
            {
                LoggingService.LogDebug($"Frame {frameCount}, DeltaTime: {deltaTime:F4}s");
                LoggingService.LogDebug($"Total GameObjects: {_gameObjects.Count}");
            }
        }

        // Limpeza
        _physicsWorldBepu?.Dispose();
        _cl?.Dispose();
        _gd?.Dispose();
        _window?.Close();
        LoggingService.LogInfo("Engine shutdown complete");
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
    public RigidbodyComponent CreateRigidbody(
        float mass = 1f,
        bool isStatic = false)
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
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsWorldBepu.RaycastResult hit)
    {
        if (_physicsWorldBepu == null)
        {
            hit = default;
            return false;
        }
        
        return _physicsWorldBepu.Raycast(origin, direction, maxDistance, out hit);
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
    /// Cria um GameObject com física básica
    /// </summary>
    public GameObject CreatePhysicsGameObject(
        string name,
        Vector3 position,
        IPhysicsShape shape,
        Material material,
        float mass = 1f)
    {
        var gameObject = new GameObject(name)
        {
            Position = position
        };
        
        // Criar rigidbody básico - massa 0 ou negativa = estático
        bool isStatic = mass <= 0f;
        var rigidbody = CreateRigidbody(Math.Max(mass, 1f), isStatic);
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

    /// <summary>
    /// Cria um cubo físico simples e intuitivo
    /// </summary>
    /// <param name="name">Nome do objeto</param>
    /// <param name="position">Posição do centro do cubo</param>
    /// <param name="size">Dimensões completas do cubo (largura, altura, profundidade)</param>
    /// <param name="color">Cor do cubo</param>
    /// <param name="mass">Massa (0 = estático)</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateCube(string name, Vector3 position, Vector3 size, RgbaFloat color, float mass = 1f)
    {
        var gameObject = new GameObject(name)
        {
            Position = position,
            Scale = size // Escala visual = dimensões desejadas
        };
        
        // Criar rigidbody com física correta
        bool isStatic = mass <= 0f;
        var rigidbody = CreateRigidbody(Math.Max(mass, 1f), isStatic);
        
        // Converter dimensões completas para half-extents que o BepuPhysics espera
        var halfExtents = size;
        rigidbody.Shape = new BoxShape(halfExtents); // BepuPhysics usa half-extents
        rigidbody.Material = Material.Default;
        rigidbody.Pose = new BepuPhysics.RigidPose(position, Quaternion.Identity);
        
        LoggingService.LogInfo($"CreateCube - {name}: visual size {size}, physics halfExtents {halfExtents}, position: {position}");
        
        gameObject.AddComponent(rigidbody);
        
        // Renderização automaticamente consistente
        var renderer = CreateCubeRenderer(color);
        gameObject.AddComponent(renderer);
        
        // Adicionar ao mundo físico e cena
        _physicsWorldBepu?.AddBody(rigidbody);
        AddGameObject(gameObject);
        
        return gameObject;
    }
    
    /// <summary>
    /// Cria uma esfera física simples e intuitiva
    /// </summary>
    /// <param name="name">Nome do objeto</param>
    /// <param name="position">Posição do centro da esfera</param>
    /// <param name="radius">Raio da esfera</param>
    /// <param name="color">Cor da esfera</param>
    /// <param name="mass">Massa (0 = estático)</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateSphere(string name, Vector3 position, float radius, RgbaFloat color, float mass = 1f)
    {
        var gameObject = new GameObject(name)
        {
            Position = position,
            Scale = Vector3.One * radius // Escala visual = raio (SimpleSphere tem geometria de raio 1.0)
        };
        
        // Criar rigidbody com física correta
        bool isStatic = mass <= 0f;
        var rigidbody = CreateRigidbody(Math.Max(mass, 1f), isStatic);
        
        // TESTE: Dobrar o raio na física para compensar problema de escala
        var physicsRadius = radius * 2f;
        rigidbody.Shape = new SphereShape(physicsRadius); // TESTE: Usar raio dobrado
        rigidbody.Material = Material.Default;
        rigidbody.Pose = new BepuPhysics.RigidPose(position, Quaternion.Identity);
        
        LoggingService.LogInfo($"CreateSphere - visual radius: {radius}, physics radius: {physicsRadius}, position: {position}, scale: {Vector3.One * radius}");
        
        gameObject.AddComponent(rigidbody);
        
        // Renderização (temporariamente cubo até implementarmos esfera real)
        var renderer = CreateSphereRenderer(color);
        gameObject.AddComponent(renderer);
        
        // Adicionar ao mundo físico e cena
        _physicsWorldBepu?.AddBody(rigidbody);
        AddGameObject(gameObject);
        
        return gameObject;
    }
    
    /// <summary>
    /// Cria um chão físico simples e intuitivo
    /// </summary>
    /// <param name="position">Posição do centro do chão</param>
    /// <param name="size">Dimensões completas do chão (largura, altura, profundidade)</param>
    /// <param name="color">Cor do chão</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateGround(Vector3 position, Vector3 size, RgbaFloat color)
    {
        return CreateCube("Ground", position, size, color, 0f); // Massa 0 = estático
    }

    /// <summary>
    /// Cria um objeto estático (massa 0) - útil para chão, paredes, etc.
    /// </summary>
    /// <param name="name">Nome do objeto</param>
    /// <param name="position">Posição do centro</param>
    /// <param name="size">Dimensões completas</param>
    /// <param name="color">Cor</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateStaticCube(string name, Vector3 position, Vector3 size, RgbaFloat color)
    {
        return CreateCube(name, position, size, color, 0f); // Massa 0 = estático
    }

    /// <summary>
    /// Cria uma rampa inclinada
    /// </summary>
    /// <param name="name">Nome da rampa</param>
    /// <param name="position">Posição do centro da rampa</param>
    /// <param name="size">Dimensões da rampa (largura, altura, profundidade)</param>
    /// <param name="angleDegrees">Ângulo de inclinação em graus</param>
    /// <param name="color">Cor da rampa</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateRamp(string name, Vector3 position, Vector3 size, float angleDegrees, RgbaFloat color)
    {
        
        var gameObject = new GameObject(name)
        {
            Position = position,
            Scale = size,
            Rotation = new Vector3(0, 0, MathF.PI * angleDegrees / 180f) // Rotação no eixo Z para inclinar
        };
        
        // Criar rigidbody estático (rampas não se movem)
        var rigidbody = CreateRigidbody(1f, true); // Estático
        var halfExtents = size;
        rigidbody.Shape = new BoxShape(halfExtents);
        rigidbody.Material = Material.Default;
        
        // Aplicar a rotação na pose do rigidbody
        var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(0, 0, MathF.PI * angleDegrees / 180f);
        rigidbody.Pose = new BepuPhysics.RigidPose(position, rotationQuaternion);
        
        gameObject.AddComponent(rigidbody);
        
        // Renderização
        var renderer = CreateCubeRenderer(color);
        gameObject.AddComponent(renderer);
        
        // Adicionar ao mundo físico e cena
        _physicsWorldBepu?.AddBody(rigidbody);
        AddGameObject(gameObject);
        
        LoggingService.LogInfo($"CreateRamp - {name} at {position} with angle {angleDegrees}° and size {size}");
        
        return gameObject;
    }
    
    /// <summary>
    /// Cria um componente de renderização de cubo com iluminação
    /// </summary>
    public CubeRenderComponentLit CreateCubeRendererLit(RgbaFloat color)
    {
        if (_gd == null || _factory == null || _cl == null)
            throw new InvalidOperationException("Engine not initialized");
            
        return new CubeRenderComponentLit(_gd, _factory, _cl, color, _lightingSystem);
    }
    
    /// <summary>
    /// Cria um cubo físico com iluminação avançada
    /// </summary>
    /// <param name="name">Nome do objeto</param>
    /// <param name="position">Posição do centro do cubo</param>
    /// <param name="size">Dimensões completas do cubo</param>
    /// <param name="color">Cor do cubo</param>
    /// <param name="mass">Massa (0 = estático)</param>
    /// <param name="shininess">Brilho especular (padrão: 32)</param>
    /// <param name="specularIntensity">Intensidade especular (padrão: 0.3)</param>
    /// <returns>GameObject criado</returns>
    public GameObject CreateCubeLit(string name, Vector3 position, Vector3 size, RgbaFloat color, 
        float mass = 1f, float shininess = 32f, float specularIntensity = 0.3f)
    {
        var gameObject = new GameObject(name)
        {
            Position = position,
            Scale = size
        };
        
        // Criar rigidbody com física correta
        bool isStatic = mass <= 0f;
        var rigidbody = CreateRigidbody(Math.Max(mass, 1f), isStatic);
        
        var halfExtents = size;
        rigidbody.Shape = new BoxShape(halfExtents);
        rigidbody.Material = Material.Default;
        rigidbody.Pose = new BepuPhysics.RigidPose(position, Quaternion.Identity);
        
        gameObject.AddComponent(rigidbody);
        
        // Renderização com iluminação
        var renderer = CreateCubeRendererLit(color);
        renderer.Shininess = shininess;
        renderer.SpecularIntensity = specularIntensity;
        gameObject.AddComponent(renderer);
        
        // Adicionar ao mundo físico e cena
        _physicsWorldBepu?.AddBody(rigidbody);
        AddGameObject(gameObject);
        
        LoggingService.LogInfo($"CreateCubeLit - {name}: lit cube with shininess {shininess}, specular {specularIntensity}");
        
        return gameObject;
    }
    
    /// <summary>
    /// Cria um chão físico com iluminação
    /// </summary>
    public GameObject CreateGroundLit(Vector3 position, Vector3 size, RgbaFloat color, 
        float shininess = 8f, float specularIntensity = 0.1f)
    {
        return CreateCubeLit("Ground", position, size, color, 0f, shininess, specularIntensity);
    }
    
    /// <summary>
    /// Cria um objeto estático com iluminação
    /// </summary>
    public GameObject CreateStaticCubeLit(string name, Vector3 position, Vector3 size, RgbaFloat color,
        float shininess = 16f, float specularIntensity = 0.2f)
    {
        return CreateCubeLit(name, position, size, color, 0f, shininess, specularIntensity);
    }
    
    /// <summary>
    /// Cria um skybox procedural com nuvens em movimento
    /// </summary>
    public GameObject CreateSkybox()
    {
        if (_gd == null || _factory == null || _cl == null)
            throw new InvalidOperationException("Graphics device not initialized");

        var skyboxObject = new GameObject("Skybox");
        
        // Posicionar o skybox no centro da câmera (será sempre seguido)
        skyboxObject.Position = Vector3.Zero;
        skyboxObject.Scale = Vector3.One * 50f; // Skybox grande o suficiente
        
        // Adicionar o componente de renderização do skybox
        var skyboxRenderer = new SkyboxRenderComponent(_gd, _factory, _cl, _lightingSystem);
        skyboxObject.AddComponent(skyboxRenderer);
        
        // Adicionar à lista de objetos (skybox deve ser renderizado primeiro)
        _gameObjects.Insert(0, skyboxObject);
        
        LoggingService.LogInfo("Procedural skybox created with animated clouds");
        
        return skyboxObject;
    }
    
    /// <summary>
    /// Trata o redimensionamento da janela
    /// </summary>
    private void OnWindowResized()
    {
        if (_gd == null) return;
        
        try
        {
            // Recriar o swapchain com o novo tamanho
            _gd.MainSwapchain?.Dispose();
            _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
            
            LoggingService.LogInfo($"Swapchain resized to {_window.Width}x{_window.Height}");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error resizing window: {ex.Message}");
        }
    }

    private void Render()
    {
        if (_gd == null || _cl == null || _physicsWorldBepu == null)
            return;

        _renderFrameCount++;

        // Criar matrizes de visualização e projeção usando a câmera controlável
        var aspectRatio = (float)_gd.MainSwapchain.Framebuffer.Width / _gd.MainSwapchain.Framebuffer.Height;
        var viewMatrix = _camera.GetViewMatrix();
        var projectionMatrix = _camera.GetProjectionMatrix(aspectRatio);

        // Começar o comando de renderização
        _cl.Begin();
        _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
        _cl.ClearColorTarget(0, RgbaFloat.Black);
        _cl.ClearDepthStencil(1f);

        // Renderizar todos os GameObjects
        
        foreach (var gameObject in _gameObjects)
        {
            var renderComponent = gameObject.GetComponent<RenderComponent>();
            if (renderComponent != null)
            {
                renderComponent.Render(_cl, viewMatrix, projectionMatrix);
            }
            else if (_renderFrameCount == 1)
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