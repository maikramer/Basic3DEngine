namespace Basic3DEngine.Core;

/// <summary>
/// Gerencia informações de tempo na engine
/// </summary>
public static class Time
{
    /// <summary>
    /// Delta time fixo para simulação física (60 FPS)
    /// </summary>
    public const float FixedDeltaTime = 1f / 60f;
    
    /// <summary>
    /// Tempo desde o início da aplicação em segundos
    /// </summary>
    public static float TotalTime { get; internal set; }
    
    /// <summary>
    /// Delta time do frame atual
    /// </summary>
    public static float DeltaTime { get; internal set; }
    
    /// <summary>
    /// Escala de tempo (0 = pausado, 1 = normal, 2 = 2x mais rápido)
    /// </summary>
    public static float TimeScale { get; set; } = 1f;
    
    /// <summary>
    /// Delta time não escalado
    /// </summary>
    public static float UnscaledDeltaTime { get; internal set; }
} 