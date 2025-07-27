using System.Numerics;
using Basic3DEngine.Entities;

namespace Basic3DEngine;

/// <summary>
/// Classe base abstrata para jogos usando a Basic3DEngine
/// </summary>
public abstract class Game
{
    protected Engine? _engine;
    
    /// <summary>
    /// Chamado uma vez quando o jogo inicia
    /// </summary>
    public abstract void Initialize(Engine engine);
    
    /// <summary>
    /// Chamado a cada frame para atualizar a lógica do jogo
    /// </summary>
    public abstract void Update(float deltaTime);
    
    /// <summary>
    /// Chamado quando o jogo termina
    /// </summary>
    public virtual void Shutdown()
    {
        // Implementação padrão vazia
    }
    
    /// <summary>
    /// Adiciona um GameObject à cena
    /// </summary>
    protected void AddGameObject(GameObject gameObject)
    {
        _engine?.AddGameObject(gameObject);
    }
    
    /// <summary>
    /// Remove um GameObject da cena
    /// </summary>
    protected void RemoveGameObject(GameObject gameObject)
    {
        _engine?.RemoveGameObject(gameObject);
    }
} 