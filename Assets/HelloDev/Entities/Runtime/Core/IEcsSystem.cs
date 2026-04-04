namespace HelloDev.Entities
{
    public interface IEcsSystem
    {
        // Called once after the world is created — build masks and allocate NativeContainers here.
        void Initialize(EcsWorld world);
        // Simulation step — runs in FixedUpdate. Safe to make structural changes via commandBuffer.
        void FixedExecute(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime);
        // Visual/presentation step — runs in Update. No structural changes; no command buffer.
        void Execute(EcsWorld world, float deltaTime);
        void Dispose();
    }
}