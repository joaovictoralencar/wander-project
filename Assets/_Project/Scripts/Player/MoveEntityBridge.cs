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
    /// Owns PositionComponent, MovementStateComponent, and MovementStatsComponent.
    /// </summary>
    [RequiresSystem(typeof(CharacterPhysicsSystem))]
    [Provides(typeof(PositionComponent), typeof(MovementStateComponent), typeof(MovementStatsComponent))]
    [RequireComponent(typeof(CharacterController))]
    public class MoveEntityBridge : EcsComponentBridge
    {
        [Header("Movement Stats")]
        [SerializeField] private MovementStatsComponent _stats = new()
        {
            WalkSpeed     = 4f,
            RunSpeed      = 8f,
            JumpForce     = 7f,
            Gravity       = 18f,
            RotationSpeed = 12f,
        };

        private CharacterController _characterController;

        private void Awake() => _characterController = GetComponent<CharacterController>();

        protected override void OnInitialize()
        {
            Add(new PositionComponent());
            Add(new MovementStateComponent { IsGrounded = true, CanMove = true });
            Add(_stats);
        }

        protected override void OnPushToEcs()
        {
            var state = Get<MovementStateComponent>();
            state.IsGrounded = _characterController.isGrounded;
            Set(state);
        }

        protected override void OnFixedPullFromEcs()
        {
            var state = Get<MovementStateComponent>();

            if (!state.CanMove)
            {
                Add(new PositionComponent { Value = (float3)transform.position });
                return;
            }

            var stats = Get<MovementStatsComponent>();

            _characterController.Move((Vector3)state.Velocity * Time.fixedDeltaTime);

            float3 horizontal = new float3(state.Velocity.x, 0f, state.Velocity.z);
            if (math.lengthsq(horizontal) > 0.01f)
            {
                var targetRot = Quaternion.LookRotation((Vector3)math.normalize(horizontal));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.RotationSpeed * Time.fixedDeltaTime);
            }

            Add(new PositionComponent { Value = (float3)transform.position });
        }
    }
}
