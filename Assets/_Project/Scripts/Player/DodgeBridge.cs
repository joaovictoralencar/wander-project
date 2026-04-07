using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Systems;

namespace Wander
{
    [RequiresSystem(typeof(DodgeSystem))]
    [Provides(typeof(DodgeComponent))]
    public class DodgeBridge : EcsComponentBridge
    {
        [SerializeField] private DodgeComponent _dodge = new() { DodgeDuration = 0.4f, DodgeSpeed = 8f };
        [SerializeField] private AnimationCurve _dodgeAccelCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private CharacterController _characterController;

        private void Awake()
        {
            if (_characterController == null) TryGetComponent(out _characterController);
        }

        protected override void OnInitialize() => Add(_dodge);

        protected override void OnFixedPullFromEcs()
        {
            var dodge = Get<DodgeComponent>();
            if (!dodge.IsDodging || _characterController == null) return;

            var   state     = Get<MovementStateComponent>();
            float t         = dodge.DodgeDuration > 0f ? dodge.ElapsedTime / dodge.DodgeDuration : 0f;
            float speedMult = _dodgeAccelCurve.Evaluate(t);

            var velocity = new UnityEngine.Vector3(
                dodge.Direction.x * dodge.DodgeSpeed * speedMult,
                state.Velocity.y,
                dodge.Direction.z * dodge.DodgeSpeed * speedMult);

            _characterController.Move(velocity * Time.fixedDeltaTime);
        }
    }
}
