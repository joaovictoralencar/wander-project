using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Events;
using Wander.Player;

namespace Wander.Character.Attack
{
    /// <summary>
    /// Place this on the same GameObject as the Animator.
    /// Animation Events call these methods; the proxy forwards them to AttackBridge.
    /// </summary>
    public class AttackAnimEventProxy : MonoBehaviour
    {
        private IAttackAnimEventReceiver _receiver;
        private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private EcsEntityRoot ecsEntityRoot;

        private IDisposable _comboStartSub;
        private IDisposable _stepStartedSub;
        private IDisposable _attackEndedSub;
        private bool _isAttacking;
        private bool _gravityDisabled;

        private void Awake()
        {
            _receiver = GetComponentInParent<IAttackAnimEventReceiver>();
            animator = GetComponent<Animator>();
            if (_receiver == null)
                Debug.LogWarning($"[AttackAnimEventProxy] No IAttackAnimEventReceiver found on parents of '{gameObject.name}'.", this);
        }

        private void Start()
        {
            _stepStartedSub = ecsEntityRoot.World.Subscribe<AttackStepStartedEvent>(OnStepStarted);
            _attackEndedSub = ecsEntityRoot.World.Subscribe<AttackEndedEvent>(OnAttackEnded);
            _comboStartSub = ecsEntityRoot.World.Subscribe<AttackComboStartEvent>(OnComboStart);
        }

        private void OnComboStart(AttackComboStartEvent obj)
        {
            _isAttacking = true;
            _gravityDisabled = false;
        }

        private void OnStepStarted(AttackStepStartedEvent attackStepStartedEvent)
        {
            _isAttacking = true;
            _gravityDisabled = false;
        }

        private void OnAttackEnded(AttackEndedEvent attackEndedEvent)
        {
            _isAttacking = false;
            _gravityDisabled = false;
        }


        private void OnDestroy()
        {
            _stepStartedSub.Dispose();
            _comboStartSub.Dispose();
            _attackEndedSub.Dispose();
        }

        // ── Called by Animation Events on attack clips ──

        public void OnComboWindowOpen() => _receiver?.OnComboWindowOpen();
        public void OnComboWindowClose() => _receiver?.OnComboWindowClose();
        public void OnHitboxActivate() => _receiver?.OnHitboxActivate();
        public void OnHitboxDeactivate() => _receiver?.OnHitboxDeactivate();
        public void OnDisableGravity() => _gravityDisabled = false;
        public void OnEnableGravity()
        {
            if (!ecsEntityRoot.World.TryGetComponent<MovementStateComponent>(ecsEntityRoot.Entity, out var moveState))
                return;

            if (moveState.IsGrounded)
                _gravityDisabled = true;
        }


        void OnAnimatorMove()
        {
            if (animator != null && characterController != null && _isAttacking)
            {
                Vector3 velocity = animator.deltaPosition;
                if (!_gravityDisabled)
                {
                    var stats = ecsEntityRoot.World.GetComponent<MovementStatsComponent>(ecsEntityRoot.Entity);
                    velocity.y -= stats.Gravity * .35f * Time.fixedDeltaTime;
                }
                characterController.Move(velocity);
            }
        }
    }
}