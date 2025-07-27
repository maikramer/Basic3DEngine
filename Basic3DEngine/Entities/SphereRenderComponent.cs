using System.Numerics;
using Basic3DEngine.Entities.Primitives;
using Veldrid;

namespace Basic3DEngine.Entities;

public class SphereRenderComponent : RenderComponent
{
    private static StreamWriter _logFile;
    private int _resolution;
    private readonly Icosphere _sphere;

    public SphereRenderComponent(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList,
        RgbaFloat color, int resolution = 2)
        : base(graphicsDevice, factory, commandList, color)
    {
        Log("Creating SphereRenderComponent with corrected Icosphere");
        _resolution = resolution;
        
        try
        {
            Log("About to create SimpleSphere...");
            // Usar SimpleSphere que funciona como Cube mas com geometria de esfera
            _sphere = new Icosphere(graphicsDevice, factory, commandList, Vector3.Zero, color);
            Log("SimpleSphere created successfully");   
        }
        catch (Exception ex)
        {
            Log($"ERROR creating SimpleSphere: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw para o sistema capturar
        }
        
        Log("SphereRenderComponent created successfully with Icosphere");
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (GameObject != null)
        {
            Log($"Rendering sphere: {GameObject.Name} at position {GameObject.Position}");
            // Atualizar a posição, rotação e escala da esfera com base no GameObject
            _sphere.Position = GameObject.Position;
            _sphere.Rotation = GameObject.Rotation;
            _sphere.Scale = GameObject.Scale;

            _sphere.Render(commandList, viewMatrix, projectionMatrix);
        }
        else
        {
            Log("SphereRenderComponent has no GameObject");
        }
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        // Atualizar a esfera se necessário
        _sphere?.Update(deltaTime);
    }

    private static void Log(string message)
    {
        // Lazy initialization of log file
        if (_logFile == null)
        {
            _logFile = new StreamWriter("/home/maikeu/MeusProgramas/TestQwen/sphere_render_debug.log", false);
            _logFile.AutoFlush = true;
        }

        _logFile.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}