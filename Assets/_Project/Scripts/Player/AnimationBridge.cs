using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Events;
using Wander.Character.Systems;

namespace Wander.Player
{
    /// <summary>
    /// Reads <see cref="AnimationStateComponent"/> for continuous state (speed, grounded)
    /// and subscribes to events (<see cref="JumpStartedEvent"/>, <see cref="DodgeStartedEvent"/>)
    /// for one-shot triggers.
    /// </summary>
    [RequiresSystem(typeof(AnimationStateSystem))]
    [Provides(typeof(AnimationStateComponent))]
    public class AnimationBridge : EcsComponentBridge
    {
        [Header("Animator Parameter Names")]
        [SerializeField] private string _speedParam    = "Speed";
        [SerializeField] private string _groundedParam = "Grounded";
        [SerializeField] private string _jumpParam     = "Jump";
        [SerializeField] private string _freeFallParam = "FreeFall";
        [SerializeField] private string _dodgeParam    = "Dodge";

        [SerializeField] private Animator _animator;

        private int _speedId;
        private int _groundedId;
        private int _jumpId;
        private int _freeFallId;
        private int _dodgeId;

        private IDisposable _jumpSub;
        private IDisposable _dodgeSub;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogWarning($"[AnimationBridge] No Animator found on '{gameObject.name}' or its children.", this);
                return;
            }

            _speedId    = Animator.StringToHash(_speedParam);
            _groundedId = Animator.StringToHash(_groundedParam);
            _jumpId     = Animator.StringToHash(_jumpParam);
            _freeFallId = Animator.StringToHash(_freeFallParam);
            _dodgeId    = Animator.StringToHash(_dodgeParam);

#if UNITY_EDITOR
            ValidateAnimatorParams();
#endif
        }

#if UNITY_EDITOR
        private void ValidateAnimatorParams()
        {
            if (_animator.runtimeAnimatorController == null) return;

            var paramNames = new System.Collections.Generic.HashSet<string>();
            foreach (var p in _animator.parameters)
                paramNames.Add(p.name);

            foreach (var name in new[] { _speedParam, _groundedParam, _jumpParam, _freeFallParam, _dodgeParam })
                if (!paramNames.Contains(name))
                    Debug.LogWarning($"[AnimationBridge] Animator on '{gameObject.name}' has no parameter '{name}'.", this);
        }
#endif

        protected override void OnInitialize()
        {
            Add(new AnimationStateComponent { IsGrounded = true });

            _jumpSub = World.Subscribe<JumpStartedEvent>(e =>
            {
                if (e.Entity == Entity && _animator != null)
                    _animator.SetTrigger(_jumpId);
            });

            _dodgeSub = World.Subscribe<DodgeStartedEvent>(e =>
            {
                if (e.Entity == Entity && _animator != null)
                    _animator.SetTrigger(_dodgeId);
            });
        }

        protected override void OnPullFromEcs()
        {
            if (_animator == null) return;

            var anim = Get<AnimationStateComponent>();

            _animator.SetFloat(_speedId,   anim.SpeedBlend);
            _animator.SetBool(_groundedId, anim.IsGrounded);
            _animator.SetBool(_freeFallId, !anim.IsGrounded);
        }

        protected override void OnDestroy()
        {
            _jumpSub?.Dispose();
            _dodgeSub?.Dispose();
            base.OnDestroy();
        }
    }
}
