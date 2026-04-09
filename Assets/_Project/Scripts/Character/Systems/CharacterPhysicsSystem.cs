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
    /// Computes full character movement each physics step:
    ///   1. Horizontal — from input direction and movement stats.
    ///   2. Vertical   — gravity accumulation or jump force, based on grounded state.
    /// Writes the result into MovementStateComponent; the bridge applies it via CharacterController.
    /// </summary>
    [Serializable]
    public class CharacterPhysicsSystem : EcsSystemBase
    {
        // Runs AFTER Dodge (0) and Attack (5) so it can read their flags
        // and act as the single resolver for derived movement state (CanMove, Speed).
        public override int Order => 50;

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

                // Resolve CanMove from ability flags — single authority for this derived value.
                bool canMove = true;
                if (world.TryGetComponent<DodgeComponent>(entity, out var dodge) && dodge.IsDodging)
                    canMove = false;
                if (world.TryGetComponent<AttackComponent>(entity, out var attack) && attack.IsAttacking)
                    canMove = false;

                float3 horizontal = float3.zero;
                float  speed      = 0f;
                if (canMove)
                {
                    float targetSpeed   = input.Sprint ? stats.RunSpeed : stats.WalkSpeed;
                    float inputStrength = math.saturate(math.length(input.Direction));
                    horizontal = math.normalizesafe(input.Direction) * targetSpeed * inputStrength;
                    speed      = math.length(horizontal);
                }

                // Vertical
                float verticalVelocity = state.Velocity.y;
                if (state.IsGrounded)
                {
                    if (input.Jump && canMove)
                    {
                        verticalVelocity = stats.JumpForce;
                        world.Send(new JumpStartedEvent { Entity = entity });
                    }
                    else
                    {
                        verticalVelocity = -2f;
                    }
                }
                else
                    verticalVelocity -= stats.Gravity * fixedDeltaTime;

                world.SetComponent(entity, new MovementStateComponent
                {
                    Velocity   = new float3(horizontal.x, verticalVelocity, horizontal.z),
                    Speed      = speed,
                    IsGrounded = state.IsGrounded,
                    CanMove    = canMove,
                });
            }
        }
    }
}
