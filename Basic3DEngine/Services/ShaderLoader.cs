using System.IO;
using System.Text;
using Veldrid;

namespace Basic3DEngine.Services;

/// <summary>
/// Utilitário para carregar shaders de arquivos
/// </summary>
public static class ShaderLoader
{
    /// <summary>
    /// Carrega um shader de um arquivo
    /// </summary>
    /// <param name="factory">Factory para criar o shader</param>
    /// <param name="shaderPath">Caminho do arquivo shader</param>
    /// <param name="stage">Estágio do shader (Vertex, Fragment, etc.)</param>
    /// <param name="entryPoint">Ponto de entrada (geralmente "main")</param>
    /// <returns>Shader carregado</returns>
    public static Shader LoadShader(ResourceFactory factory, string shaderPath, ShaderStages stage, string entryPoint = "main")
    {
        if (!File.Exists(shaderPath))
        {
            throw new FileNotFoundException($"Shader não encontrado: {shaderPath}");
        }

        var shaderCode = File.ReadAllText(shaderPath, Encoding.UTF8);
        var shaderBytes = Encoding.UTF8.GetBytes(shaderCode);

        var shaderDescription = new ShaderDescription(stage, shaderBytes, entryPoint);
        return factory.CreateShader(shaderDescription);
    }

    /// <summary>
    /// Carrega um par de shaders vertex/fragment de uma pasta
    /// </summary>
    /// <param name="factory">Factory para criar os shaders</param>
    /// <param name="shaderFolder">Pasta contendo os shaders</param>
    /// <param name="vertexFileName">Nome do arquivo vertex shader</param>
    /// <param name="fragmentFileName">Nome do arquivo fragment shader</param>
    /// <returns>Array com [VertexShader, FragmentShader]</returns>
    public static Shader[] LoadShaderPair(ResourceFactory factory, string shaderFolder, 
                                          string vertexFileName = "vertex.glsl", 
                                          string fragmentFileName = "fragment.glsl")
    {
        var vertexPath = Path.Combine(shaderFolder, vertexFileName);
        var fragmentPath = Path.Combine(shaderFolder, fragmentFileName);

        var vertexShader = LoadShader(factory, vertexPath, ShaderStages.Vertex);
        var fragmentShader = LoadShader(factory, fragmentPath, ShaderStages.Fragment);

        return new[] { vertexShader, fragmentShader };
    }

    /// <summary>
    /// Obtém o caminho base da pasta Shaders
    /// </summary>
    /// <returns>Caminho da pasta Shaders</returns>
    public static string GetShadersBasePath()
    {
        // Procura pela pasta Shaders na hierarquia de diretórios
        var currentDir = Directory.GetCurrentDirectory();
        
        // Primeiro tenta procurar Shaders no diretório atual e pais
        var searchDir = currentDir;
        while (searchDir != null && !Directory.Exists(Path.Combine(searchDir, "Shaders")))
        {
            var parent = Directory.GetParent(searchDir);
            if (parent == null) break;
            searchDir = parent.FullName;
        }

        // Se não encontrou, tenta procurar na pasta Basic3DEngine
        if (searchDir == null || !Directory.Exists(Path.Combine(searchDir, "Shaders")))
        {
            searchDir = currentDir;
            
            // Procura por Basic3DEngine/Shaders
            while (searchDir != null)
            {
                var basic3DEngineShaders = Path.Combine(searchDir, "Basic3DEngine", "Shaders");
                if (Directory.Exists(basic3DEngineShaders))
                {
                    return basic3DEngineShaders;
                }
                
                var parent = Directory.GetParent(searchDir);
                if (parent == null) break;
                searchDir = parent.FullName;
            }
        }
        else
        {
            return Path.Combine(searchDir, "Shaders");
        }

        throw new DirectoryNotFoundException($"Pasta Shaders não encontrada na hierarquia a partir de: {currentDir}");
    }
} 