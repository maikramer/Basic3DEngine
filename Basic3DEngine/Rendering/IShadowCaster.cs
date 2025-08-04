using Veldrid;
using System.Numerics;

namespace Basic3DEngine.Rendering
{
    /// <summary>
    /// Interface para objetos que podem projetar sombras
    /// </summary>
    public interface IShadowCaster
    {
        /// <summary>
        /// Renderiza apenas a geometria para shadow mapping
        /// </summary>
        /// <param name="commandList">Command list para renderização</param>
        /// <param name="worldMatrix">Matrix de transformação do objeto</param>
        void RenderShadowGeometry(CommandList commandList, Matrix4x4 worldMatrix);
        
        /// <summary>
        /// Se este objeto deve projetar sombras
        /// </summary>
        bool CastsShadows { get; }
    }
}