using System.Numerics;
using Veldrid;

namespace Basic3DEngine.Services;

/// <summary>
/// Serviço de input que abstrai completamente o SDL2 e oferece uma API simples
/// </summary>
public static class InputService
{
    private static InputSnapshot? _currentSnapshot;
    private static InputSnapshot? _previousSnapshot;
    
    // Estado atual das teclas e botões
    private static readonly HashSet<Key> _currentlyPressedKeys = new();
    private static readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new();
    
    // Eventos do frame atual
    private static readonly HashSet<Key> _pressedKeys = new();
    private static readonly HashSet<Key> _releasedKeys = new();
    private static readonly HashSet<MouseButton> _pressedMouseButtons = new();
    private static readonly HashSet<MouseButton> _releasedMouseButtons = new();
    
    private static Vector2 _mousePosition = Vector2.Zero;
    private static Vector2 _mouseDelta = Vector2.Zero;
    private static Vector2 _previousMousePosition = Vector2.Zero;
    private static float _mouseWheelDelta = 0f;
    
    // Mouse capture
    private static bool _isMouseCaptured = false;
    private static bool _firstMouseUpdate = true;

    /// <summary>
    /// Atualiza o estado do input (chamado pela Engine)
    /// </summary>
    public static void Update(InputSnapshot snapshot)
    {
        _previousSnapshot = _currentSnapshot;
        _currentSnapshot = snapshot;
        
        // Limpar estados de frame anterior
        _pressedKeys.Clear();
        _releasedKeys.Clear();
        _pressedMouseButtons.Clear();
        _releasedMouseButtons.Clear();
        _mouseWheelDelta = 0f;
        
        if (_currentSnapshot == null) return;
        
        // Detectar teclas pressionadas/soltas
        for (int i = 0; i < _currentSnapshot.KeyEvents.Count; i++)
        {
            var keyEvent = _currentSnapshot.KeyEvents[i];
            if (keyEvent.Down)
            {
                _currentlyPressedKeys.Add(keyEvent.Key);
                _pressedKeys.Add(keyEvent.Key);
            }
            else
            {
                _currentlyPressedKeys.Remove(keyEvent.Key);
                _releasedKeys.Add(keyEvent.Key);
            }
        }
        
        // Detectar mouse buttons pressionados/soltos
        for (int i = 0; i < _currentSnapshot.MouseEvents.Count; i++)
        {
            var mouseEvent = _currentSnapshot.MouseEvents[i];
            if (mouseEvent.Down)
            {
                _currentlyPressedMouseButtons.Add(mouseEvent.MouseButton);
                _pressedMouseButtons.Add(mouseEvent.MouseButton);
            }
            else
            {
                _currentlyPressedMouseButtons.Remove(mouseEvent.MouseButton);
                _releasedMouseButtons.Add(mouseEvent.MouseButton);
            }
        }
        
        // Atualizar posição do mouse e delta
        _previousMousePosition = _mousePosition;
        _mousePosition = _currentSnapshot.MousePosition;
        
        // Prevenir "jump" na primeira atualização ou quando mouse capture muda
        if (_firstMouseUpdate || !_isMouseCaptured)
        {
            _mouseDelta = Vector2.Zero;
            _firstMouseUpdate = false;
        }
        else
        {
            _mouseDelta = _mousePosition - _previousMousePosition;
        }
        
        // Wheel delta
        _mouseWheelDelta = _currentSnapshot.WheelDelta;
    }
    
    // === TECLADO ===
    
    /// <summary>
    /// Verifica se uma tecla está sendo mantida pressionada
    /// </summary>
    public static bool IsKeyDown(Key key)
    {
        return _currentlyPressedKeys.Contains(key);
    }
    
    /// <summary>
    /// Verifica se uma tecla foi pressionada neste frame
    /// </summary>
    public static bool IsKeyPressed(Key key)
    {
        return _pressedKeys.Contains(key);
    }
    
    /// <summary>
    /// Verifica se uma tecla foi solta neste frame
    /// </summary>
    public static bool IsKeyReleased(Key key)
    {
        return _releasedKeys.Contains(key);
    }
    
    // === MOUSE ===
    
    /// <summary>
    /// Posição atual do mouse em coordenadas de tela
    /// </summary>
    public static Vector2 MousePosition => _mousePosition;
    
    /// <summary>
    /// Delta do movimento do mouse desde o último frame
    /// </summary>
    public static Vector2 MouseDelta => _mouseDelta;
    
    /// <summary>
    /// Delta da roda do mouse neste frame
    /// </summary>
    public static float MouseWheelDelta => _mouseWheelDelta;
    
    /// <summary>
    /// Verifica se um botão do mouse está sendo mantido pressionado
    /// </summary>
    public static bool IsMouseButtonDown(MouseButton button)
    {
        return _currentlyPressedMouseButtons.Contains(button);
    }
    
    /// <summary>
    /// Verifica se um botão do mouse foi pressionado neste frame
    /// </summary>
    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return _pressedMouseButtons.Contains(button);
    }
    
    /// <summary>
    /// Verifica se um botão do mouse foi solto neste frame
    /// </summary>
    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return _releasedMouseButtons.Contains(button);
    }
    
    // === MÉTODOS CONVENIENTES ===
    
    /// <summary>
    /// Retorna um vetor de movimento baseado em WASD
    /// </summary>
    public static Vector3 GetMovementVector()
    {
        var movement = Vector3.Zero;
        
        if (IsKeyDown(Key.W)) movement.Z -= 1f; // Forward
        if (IsKeyDown(Key.S)) movement.Z += 1f; // Backward
        if (IsKeyDown(Key.A)) movement.X -= 1f; // Left
        if (IsKeyDown(Key.D)) movement.X += 1f; // Right
        if (IsKeyDown(Key.Space)) movement.Y += 1f; // Up
        if (IsKeyDown(Key.LShift)) movement.Y -= 1f; // Down
        
        return Vector3.Normalize(movement.LengthSquared() > 0 ? movement : Vector3.Zero);
    }
    
    /// <summary>
    /// Verifica se a tecla Escape foi pressionada
    /// </summary>
    public static bool IsExitRequested()
    {
        return IsKeyPressed(Key.Escape);
    }
    
    // === MOUSE CAPTURE ===
    
    /// <summary>
    /// Ativa/desativa mouse capture para FPS controls
    /// </summary>
    public static void SetMouseCaptured(bool captured)
    {
        if (_isMouseCaptured != captured)
        {
            _isMouseCaptured = captured;
            _firstMouseUpdate = true; // Reset para evitar jump
            LoggingService.LogInfo($"Mouse capture: {(_isMouseCaptured ? "ON" : "OFF")}");
        }
    }
    
    /// <summary>
    /// Verifica se o mouse está capturado
    /// </summary>
    public static bool IsMouseCaptured => _isMouseCaptured;
} 