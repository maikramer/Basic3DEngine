using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using BepuShapes = BepuPhysics.Collidables.Shapes;

namespace Basic3DEngine.Core.Interfaces;

/// <summary>
/// Interface para shapes que exigem acesso ao BufferPool para criar-se (ex.: Compound)
/// </summary>
public interface IRequiresBufferPoolShape : IPhysicsShape
{
    /// <summary>
    /// Cria o shape no BepuPhysics usando também o BufferPool (necessário para composições)
    /// </summary>
    TypedIndex CreateShape(BepuShapes shapes, BufferPool pool);
}


