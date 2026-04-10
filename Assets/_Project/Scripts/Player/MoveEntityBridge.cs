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
    [Provides(typeof(TransformSnapshotComponent), typeof(MovementStateComponent), typeof(MovementStatsComponent))]
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
            Add(new MovementStateComponent { IsGrounded = true, CanMove = true });
            Add(_stats);
            Add(new TransformSnapshotComponent());
            WriteTransformSnapshot();
        }

        protected override void OnPushToEcs()
        {
            using var state = Modify<MovementStateComponent>();
            state.Value.IsGrounded = _characterController.isGrounded;
            WriteTransformSnapshot();
        }

        protected override void OnFixedPullFromEcs()
        {
            var state = Get<MovementStateComponent>();

            if (!state.CanMove)
            {
                WriteTransformSnapshot();
                return;
            }
            
            _characterController.Move((Vector3)state.Velocity * Time.fixedDeltaTime);

            float3 horizontal = new float3(state.Velocity.x, 0f, state.Velocity.z);
            
            var stats = Get<MovementStatsComponent>();
            
            if (math.lengthsq(horizontal) > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation((Vector3)math.normalize(horizontal));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.RotationSpeed * Time.fixedDeltaTime);
            }

            WriteTransformSnapshot();
            
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, math.normalize(horizontal) * 2f, Color.black, 0f, true);
        }

        private void WriteTransformSnapshot()
        {
            Set(new TransformSnapshotComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Forward = transform.forward,
                Right = transform.right,
                Scale = transform.lossyScale,
            });
        }
    }
}
