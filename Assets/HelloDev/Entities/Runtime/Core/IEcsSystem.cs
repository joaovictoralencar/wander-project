using System.Collections.Generic;

namespace HelloDev.Entities
{
    public interface IEcsSystem
    {
        // Called once after the world is created.
        void Initialize(EcsWorld world);
        // Simulation step — runs in FixedUpdate.
        void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime);
        // Presentation step — runs in Update. No structural changes.
        void Execute(EcsWorld world, List<int> entities, float deltaTime);
        void Dispose();
        // Used by the runner to filter entities. Implement via EcsSystemBase.RequiredComponents.
        long GetRequiredMask();
    }
}