namespace Basic3DEngine;

/// <summary>
/// Singleton para expor a referência da Engine a componentes que precisem de acesso ao contexto.
/// </summary>
public static class EngineSingleton
{
    public static Engine? Instance { get; set; }
}


