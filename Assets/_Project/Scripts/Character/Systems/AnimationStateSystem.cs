using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Unity.Mathematics;
using Wander.Character.Components;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Visual-pass system (runs in Update). Reads MovementStateComponent + MovementStatsComponent
    /// and writes AnimationStateComponent so the animator bridge can drive the Animator
    /// without knowing anything about movement logic.
    ///
    /// One-shot triggers (jump, dodge) are handled by event subscriptions in the bridge —
    /// this system only computes continuous blend values.
    /// </summary>
    [Serializable]
    public class AnimationStateSystem : EcsSystemBase
    {
        public override int Order => 100;

        public override void Initialize(EcsWorld world) { }

        public override void Execute(EcsWorld world, List<int> entities, float deltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = world.GetEntity(entities[i]);
                var state  = world.GetComponent<MovementStateComponent>(entity);
                var stats  = world.GetComponent<MovementStatsComponent>(entity);

                float maxSpeed   = stats.RunSpeed > 0f ? stats.RunSpeed : 1f;
                float speedBlend = math.saturate(state.Speed / maxSpeed);

                world.SetComponent(entity, new AnimationStateComponent
                {
                    SpeedBlend = speedBlend,
                    IsGrounded = state.IsGrounded,
                });
            }
        }

        public override Type[] RequiredComponents => new[]
        {
            typeof(MovementStateComponent),
            typeof(MovementStatsComponent),
            typeof(AnimationStateComponent),
        };
    }
}
