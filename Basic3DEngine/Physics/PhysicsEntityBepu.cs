using Basic3DEngine.Entities;
using BepuPhysics;

namespace Basic3DEngine.Physics;

public class PhysicsEntityBepu
{
    public PhysicsEntityBepu(GameObject gameObject, BodyHandle bodyHandle, bool isStatic = false)
    {
        GameObject = gameObject;
        BodyHandle = bodyHandle;
        IsStatic = isStatic;
    }

    public GameObject GameObject { get; set; }
    public BodyHandle BodyHandle { get; set; }
    public bool IsStatic { get; set; }

    // Atualizar a posição do GameObject com base na posição do corpo físico
    public void UpdatePosition(Simulation simulation)
    {
        if (!IsStatic && GameObject != null)
        {
            var bodyReference = simulation.Bodies[BodyHandle];
            var pose = bodyReference.Pose;
            GameObject.Position = pose.Position;
        }
    }
}