using System.Numerics;
using Veldrid;

namespace Basic3DEngine.Entities;

public class CubeRenderComponent : RenderComponent
{
    private static StreamWriter _logFile;
    private readonly Cube _cube;
    private readonly OutputDescription? _targetOutputDescription;

    public CubeRenderComponent(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList,
        RgbaFloat color, OutputDescription? targetOutputDescription = null)
        : base(graphicsDevice, factory, commandList, color)
    {
        Log("Creating CubeRenderComponent");
        _targetOutputDescription = targetOutputDescription;
        _cube = new Cube(graphicsDevice, factory, commandList, Vector3.Zero, color, _targetOutputDescription);
        Log("CubeRenderComponent created successfully");
    }

    public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        if (GameObject != null)
        {
            Log($"Rendering cube: {GameObject.Name} at position {GameObject.Position}");
            // Atualizar a posição, rotação e escala do cubo com base no GameObject
            _cube.Position = GameObject.Position;
            _cube.Rotation = GameObject.Rotation;
            _cube.Scale = GameObject.Scale;

            _cube.Render(commandList, viewMatrix, projectionMatrix);
        }
        else
        {
            Log("CubeRenderComponent has no GameObject");
        }
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        // Atualizar o cubo se necessário
        _cube?.Update(deltaTime);
    }

    private static void Log(string message)
    {
        // Lazy initialization of log file
        if (_logFile == null)
        {
            _logFile = new StreamWriter("/home/maikeu/MeusProgramas/TestQwen/cube_render_debug.log", false);
            _logFile.AutoFlush = true;
        }

        _logFile.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}