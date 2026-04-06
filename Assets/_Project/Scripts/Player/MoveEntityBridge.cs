using System;
using HelloDev.Entities;
using Unity.Mathematics;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Systems;
using Wander.Components;

namespace Wander.Player
{
    /// <summary>
    /// Translates between Unity's CharacterController and ECS movement state.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MoveEntityBridge : EcsComponentBridge
    {
        private CharacterController _characterController;

        private void Awake() => _characterController = GetComponent<CharacterController>();

        public override Type[] RequiredSystems => new[] { typeof(CharacterPhysicsSystem) };

        public override Type[] ProvidedComponents => new[]
        {
            typeof(PositionComponent),
            typeof(MovementStateComponent),
        };

        protected override void OnInitialize()
        {
            if (!World.HasComponent<PositionComponent>(Entity))
                World.AddComponent(Entity, new PositionComponent { Value = (float3)transform.position });

            if (!World.HasComponent<MovementStateComponent>(Entity))
                World.AddComponent(Entity, new MovementStateComponent { IsGrounded = true });
        }

        // Seed IsGrounded from CharacterController before systems run.
        protected override void OnPushToEcs()
        {
            var state = World.GetComponent<MovementStateComponent>(Entity);
            state.IsGrounded = _characterController.isGrounded;
            World.SetOrAddComponent(Entity, state);
        }

        // After CharacterPhysicsSystem has written the full velocity, apply it and sync position.
        protected override void OnFixedPullFromEcs()
        {
            var state = World.GetComponent<MovementStateComponent>(Entity);
            var stats = World.GetComponent<MovementStatsComponent>(Entity);

            _characterController.Move((Vector3)state.Velocity * Time.fixedDeltaTime);

            float3 horizontal = new float3(state.Velocity.x, 0f, state.Velocity.z);
            if (math.lengthsq(horizontal) > 0.01f)
            {
                var targetRot = Quaternion.LookRotation((Vector3)math.normalize(horizontal));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.RotationSpeed * Time.fixedDeltaTime);
            }

            World.SetOrAddComponent(Entity, new PositionComponent { Value = (float3)transform.position });
        }
    }
}

