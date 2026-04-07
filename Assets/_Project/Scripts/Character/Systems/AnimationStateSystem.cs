using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Unity.Mathematics;
using Wander;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Visual-pass system (runs in Update). Reads MovementStateComponent + MovementStatsComponent
    /// and writes AnimationStateComponent so the animator bridge can drive the Animator
    /// without knowing anything about movement logic.
    ///
    /// TriggerJump mirrors the JumpStartedEvent fired by CharacterPhysicsSystem — it stays true
    /// for all Update frames until the next FixedUpdate clears events.
    /// TriggerDodge fires for exactly one Update frame when a dodge begins.
    /// </summary>
    [Serializable]
    public class AnimationStateSystem : EcsSystemBase
    {
        public override int Order => 100;

        public override void Initialize(EcsWorld world) { }

        public override void Execute(EcsWorld world, List<int> entities, float deltaTime)
        {
            // JumpStartedEvent is fired by CharacterPhysicsSystem (FixedExecute) and lives until
            // the next FixedUpdate — any Update frame in that window sees it and triggers the animator.
            bool jumpFired = world.ReadEvents<JumpStartedEvent>().Count > 0;

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = world.GetEntity(entities[i]);
                var state  = world.GetComponent<MovementStateComponent>(entity);
                var stats  = world.GetComponent<MovementStatsComponent>(entity);
                var prev   = world.GetComponent<AnimationStateComponent>(entity);
                var dodge  = world.GetComponent<DodgeComponent>(entity);

                float maxSpeed   = stats.RunSpeed > 0f ? stats.RunSpeed : 1f;
                float speedBlend = math.saturate(state.Speed / maxSpeed);
                bool  triggerDodge = dodge.IsDodging && !prev.IsDodging;

                world.SetComponent(entity, new AnimationStateComponent
                {
                    SpeedBlend   = speedBlend,
                    IsGrounded   = state.IsGrounded,
                    TriggerJump  = jumpFired,
                    TriggerDodge = triggerDodge,
                    IsDodging    = dodge.IsDodging,
                });
            }
        }

        public override Type[] RequiredComponents => new[]
        {
            typeof(MovementStateComponent),
            typeof(MovementStatsComponent),
            typeof(AnimationStateComponent),
            typeof(DodgeComponent),
        };
    }
}
