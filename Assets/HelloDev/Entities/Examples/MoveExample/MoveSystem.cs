using HelloDev.Entities.Components;

namespace HelloDev.Entities.Examples
{
    public class MoveSystem : IEcsSystem
    {
        private int _requiredMask;

        public void Initialize(EcsWorld world)
        {
            _requiredMask = world.Registry.BuildMask(
                typeof(PositionComponent),
                typeof(VelocityComponent));
        }

        public void FixedExecute(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            var entities = world.GetEntitiesWithMask(_requiredMask);
            foreach (int id in entities)
            {
                var entity = world.GetEntity(id);
                var pos = world.GetComponent<PositionComponent>(entity);
                var vel = world.GetComponent<VelocityComponent>(entity);

                // Snapshot position before moving so the bridge can interpolate visually.
                if (world.HasComponent<PreviousPositionComponent>(entity))
                    world.SetComponent(entity, new PreviousPositionComponent { Value = pos.Value });

                pos.Value += vel.Value * vel.Speed * fixedDeltaTime;
                world.SetComponent(entity, pos);
            }
        }

        public void Execute(EcsWorld world, float deltaTime) { }

        public void Dispose() { }
    }
}