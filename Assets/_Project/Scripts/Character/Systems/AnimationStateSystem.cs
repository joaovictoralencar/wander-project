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
    /// TriggerJump fires for exactly one Update frame when the entity first leaves the ground.
    /// </summary>
    [Serializable]
    public class AnimationStateSystem : EcsSystemBase
    {
        public override void Initialize(EcsWorld world) { }

        public override void Execute(EcsWorld world, List<int> entities, float deltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = world.GetEntity(entities[i]);
                var state  = world.GetComponent<MovementStateComponent>(entity);
                var stats  = world.GetComponent<MovementStatsComponent>(entity);
                var prev   = world.GetComponent<AnimationStateComponent>(entity);

                float maxSpeed  = stats.RunSpeed > 0f ? stats.RunSpeed : 1f;
                float speedBlend = math.saturate(state.Speed / maxSpeed);
                bool triggerJump = !state.IsGrounded && prev.IsGrounded;

                world.SetComponent(entity, new AnimationStateComponent
                {
                    SpeedBlend  = speedBlend,
                    IsGrounded  = state.IsGrounded,
                    TriggerJump = triggerJump,
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
