using System.Numerics;
using Basic3DEngine.Core;
using Basic3DEngine.Services;

namespace Basic3DEngine.Core;

/// <summary>
/// Sistema de câmera com controles FPS-style
/// </summary>
public class Camera
{
    private Vector3 _position;
    private Vector3 _forward;
    private Vector3 _up;
    private Vector3 _right;
    
    private float _pitch = 0f; // Rotação vertical (radianos)
    private float _yaw = 0f;   // Rotação horizontal (radianos)
    
    // Configurações
    public float MovementSpeed { get; set; } = 10f;
    public float MouseSensitivity { get; set; } = 0.002f;
    public float MinPitch { get; set; } = -MathF.PI * 0.49f; // ~-88 graus
    public float MaxPitch { get; set; } = MathF.PI * 0.49f;  // ~88 graus
    
    // Propriedades públicas
    public Vector3 Position 
    { 
        get => _position; 
        set 
        { 
            _position = value;
            UpdateVectors();
        } 
    }
    
    public Vector3 Forward => _forward;
    public Vector3 Up => _up;
    public Vector3 Right => _right;
    
    public float Pitch 
    { 
        get => _pitch; 
        set 
        { 
            _pitch = MathF.Max(MinPitch, MathF.Min(MaxPitch, value));
            UpdateVectors();
        } 
    }
    
    public float Yaw 
    { 
        get => _yaw; 
        set 
        { 
            _yaw = value;
            UpdateVectors();
        } 
    }

    public Camera(Vector3 position, float yaw = 0f, float pitch = 0f)
    {
        _position = position;
        _yaw = yaw;
        _pitch = pitch;
        UpdateVectors();
    }

    /// <summary>
    /// Atualiza a câmera com base no input (chamado pela Engine)
    /// </summary>
    public void Update(float deltaTime)
    {
        // Controle de mouse para rotação (só quando capturado)
        if (InputService.IsMouseCaptured)
        {
            var mouseDelta = InputService.MouseDelta;
            if (mouseDelta.LengthSquared() > 0)
            {
                Yaw += mouseDelta.X * MouseSensitivity;
                Pitch -= mouseDelta.Y * MouseSensitivity; // Invertido para sentir mais natural
            }
        }
        
        // Controle de teclado para movimento
        var movementInput = InputService.GetMovementVector();
        if (movementInput.LengthSquared() > 0)
        {
            var velocity = MovementSpeed * deltaTime;
            
            // Movimento em relação à orientação da câmera
            _position += movementInput.Z * _forward * velocity; // Forward/Back
            _position += movementInput.X * _right * velocity;   // Left/Right
            _position += movementInput.Y * Vector3.UnitY * velocity; // Up/Down (sempre em Y global)
        }
        
        // Controle de velocidade com scroll do mouse
        var wheelDelta = InputService.MouseWheelDelta;
        if (wheelDelta != 0)
        {
            MovementSpeed = MathF.Max(1f, MovementSpeed + wheelDelta * 2f);
            LoggingService.LogInfo($"Camera speed: {MovementSpeed:F1}");
        }
    }
    
    /// <summary>
    /// Retorna a matriz de visualização (view matrix)
    /// </summary>
    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(_position, _position + _forward, _up);
    }
    
    /// <summary>
    /// Retorna a matriz de projeção perspectiva
    /// </summary>
    public Matrix4x4 GetProjectionMatrix(float aspectRatio, float fov = MathF.PI / 3f, float nearPlane = 0.1f, float farPlane = 1000f)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlane, farPlane);
    }
    
    /// <summary>
    /// Atualiza os vetores direcionais com base nos ângulos atuais
    /// </summary>
    private void UpdateVectors()
    {
        // Calcular o vetor forward baseado em yaw e pitch
        _forward = Vector3.Normalize(new Vector3(
            MathF.Cos(_yaw) * MathF.Cos(_pitch),
            MathF.Sin(_pitch),
            MathF.Sin(_yaw) * MathF.Cos(_pitch)
        ));
        
        // Calcular right e up
        _right = Vector3.Normalize(Vector3.Cross(_forward, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _forward));
    }
    
    /// <summary>
    /// Teleporta a câmera para uma posição específica olhando para um alvo
    /// </summary>
    public void LookAt(Vector3 position, Vector3 target)
    {
        _position = position;
        var direction = Vector3.Normalize(target - position);
        
        // Calcular yaw e pitch baseado na direção
        _yaw = MathF.Atan2(direction.Z, direction.X);
        _pitch = MathF.Asin(direction.Y);
        
        UpdateVectors();
    }
    
    /// <summary>
    /// Teleporta a câmera para uma posição com orientação específica
    /// </summary>
    public void SetPositionAndRotation(Vector3 position, float yaw, float pitch)
    {
        _position = position;
        _yaw = yaw;
        _pitch = pitch;
        UpdateVectors();
    }
} 