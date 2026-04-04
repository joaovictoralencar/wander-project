using System.Collections.Generic;
using HelloDev.Entities.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace HelloDev.Entities.Examples
{
    /// <summary>
    /// Burst-compiled parallel job. For each qualifying entity it:
    ///   1. Saves the current position into PreviousPositionComponent (for bridge interpolation).
    ///   2. Computes the direction toward TargetPosition and updates VelocityComponent.Value.
    ///   3. Steps the position by velocity * speed * fixedDeltaTime.
    ///
    /// Arrays are indexed by entity ID, not by job index — [NativeDisableParallelForRestriction]
    /// is required. No data races exist because every entity ID in EntityIds is unique.
    /// </summary>
    [BurstCompile]
    public struct SeekAndMoveJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> EntityIds;
        [ReadOnly] public float3 TargetPosition;
        public float DeltaTime;

        [NativeDisableParallelForRestriction] public NativeArray<PositionComponent> Positions;
        [NativeDisableParallelForRestriction] public NativeArray<VelocityComponent> Velocities;
        [NativeDisableParallelForRestriction] public NativeArray<PreviousPositionComponent> PreviousPositions;

        public void Execute(int index)
        {
            int id = EntityIds[index];
            var pos = Positions[id];
            var vel = Velocities[id];

            // Snapshot before moving so the bridge can interpolate between fixed steps.
            PreviousPositions[id] = new PreviousPositionComponent { Value = pos.Value };

            float3 toTarget = TargetPosition - pos.Value;
            float dist = math.length(toTarget);

            vel.Value = dist > 0.05f ? toTarget / dist : float3.zero;
            Velocities[id] = vel;

            pos.Value += vel.Value * vel.Speed * DeltaTime;
            Positions[id] = pos;
        }
    }

    public class SeekAndMoveSystem : IEcsSystem
    {
        private int _requiredMask;

        /// <summary>
        /// Set this on the main thread before FixedExecute is called each fixed step.
        /// The value is copied by value into the job struct — no shared mutable state.
        /// </summary>
        public float3 TargetPosition;

        public void Initialize(EcsWorld world)
        {
            _requiredMask = world.Registry.BuildMask(
                typeof(PositionComponent),
                typeof(VelocityComponent));
        }

        public void FixedExecute(EcsWorld world, EcsCommandBuffer commandBuffer, float fixedDeltaTime)
        {
            List<int> ids = world.GetEntitiesWithMask(_requiredMask);
            if (ids.Count == 0) return;

            NativeArray<int> entityIds = new NativeArray<int>(ids.Count, Allocator.TempJob);
            for (int i = 0; i < ids.Count; i++)
                entityIds[i] = ids[i];

            SeekAndMoveJob job = new SeekAndMoveJob
            {
                EntityIds = entityIds,
                TargetPosition = TargetPosition,
                DeltaTime = fixedDeltaTime,
                Positions = world.GetComponentDataArray<PositionComponent>(),
                Velocities = world.GetComponentDataArray<VelocityComponent>(),
                PreviousPositions = world.GetComponentDataArray<PreviousPositionComponent>(),
            };

            job.Schedule(ids.Count, 16).Complete();
            entityIds.Dispose();
        }

        // No visual-pass logic needed — the bridge handles lerp via PreviousPositionComponent.
        public void Execute(EcsWorld world, float deltaTime) { }

        public void Dispose() { }
    }
}
