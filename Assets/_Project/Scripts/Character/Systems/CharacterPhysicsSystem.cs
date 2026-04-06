using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Unity.Mathematics;
using Wander.Character.Components;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Computes full character movement each physics step:
    ///   1. Horizontal — from input direction and movement stats.
    ///   2. Vertical   — gravity accumulation or jump force, based on grounded state.
    /// Writes the result into MovementStateComponent; the bridge applies it via CharacterController.
    /// </summary>
    [Serializable]
    public class CharacterPhysicsSystem : EcsSystemBase
    {
        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(MovementStatsComponent),
            typeof(MovementStateComponent),
        };

        public override void Initialize(EcsWorld world) { }

        public override void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = world.GetEntity(entities[i]);
                var input  = world.GetComponent<MoveInputComponent>(entity);
                var stats  = world.GetComponent<MovementStatsComponent>(entity);
                var state  = world.GetComponent<MovementStateComponent>(entity);

                // Horizontal
                float targetSpeed   = input.Sprint ? stats.RunSpeed : stats.WalkSpeed;
                float inputStrength = math.saturate(math.length(input.Direction));
                float3 horizontal   = math.normalizesafe(input.Direction) * targetSpeed * inputStrength;

                // Vertical
                float verticalVelocity = state.Velocity.y;
                if (state.IsGrounded)
                    verticalVelocity = input.Jump ? stats.JumpForce : -2f;
                else
                    verticalVelocity -= stats.Gravity * fixedDeltaTime;

                world.SetOrAddComponent(entity, new MovementStateComponent
                {
                    Velocity   = new float3(horizontal.x, verticalVelocity, horizontal.z),
                    Speed      = math.length(horizontal),
                    IsGrounded = state.IsGrounded,
                });
            }
        }
    }
}
