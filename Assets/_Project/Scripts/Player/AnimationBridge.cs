using System;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;
using Wander.Character.Systems;

namespace Wander.Player
{
    /// <summary>
    /// Pure pull bridge: reads <see cref="AnimationStateComponent"/> (written by
    /// <see cref="AnimationStateSystem"/> each Update) and drives the <see cref="Animator"/>.
    ///
    /// Systems are not registered here. Add <see cref="AnimationStateSystem"/> to the
    /// <see cref="EcsEntityRoot.Systems"/> list or the <see cref="EcsSystemRunner"/> inspector.
    /// This bridge declares its dependency via <see cref="EcsComponentBridge.RequiredSystems"/>
    /// so <see cref="EcsEntityRoot"/> will warn at startup if the system is missing.
    /// </summary>
    public class AnimationBridge : EcsComponentBridge
    {
        [Header("Animator Parameter Names")]
        [SerializeField] private string _speedParam    = "Speed";
        [SerializeField] private string _groundedParam = "Grounded";
        [SerializeField] private string _jumpParam     = "Jump";
        [SerializeField] private string _freeFallParam = "FreeFall";

        // Assign in the Inspector or leave empty to auto-find on children.
        [SerializeField] private Animator _animator;

        private int _speedId;
        private int _groundedId;
        private int _jumpId;
        private int _freeFallId;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogWarning($"[AnimationBridge] No Animator found on '{gameObject.name}' or its children. Animation will be disabled.", this);
                return;
            }

            _speedId    = Animator.StringToHash(_speedParam);
            _groundedId = Animator.StringToHash(_groundedParam);
            _jumpId     = Animator.StringToHash(_jumpParam);
            _freeFallId = Animator.StringToHash(_freeFallParam);

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

            foreach (var name in new[] { _speedParam, _groundedParam, _jumpParam, _freeFallParam })
                if (!paramNames.Contains(name))
                    Debug.LogWarning($"[AnimationBridge] Animator on '{gameObject.name}' has no parameter '{name}'. Check the parameter name or the Animator Controller.", this);
        }
#endif

        public override Type[] RequiredSystems => new[] { typeof(AnimationStateSystem) };

        public override Type[] ProvidedComponents => new[] { typeof(AnimationStateComponent) };

        protected override void OnInitialize()
        {
            if (!World.HasComponent<AnimationStateComponent>(Entity))
                World.AddComponent(Entity, new AnimationStateComponent { IsGrounded = true });
        }

        // Called by EcsSystemRunner each Update, after AnimationStateSystem.Execute has written
        // the latest AnimationStateComponent values.
        protected override void OnPullFromEcs()
        {
            if (_animator == null) return;

            var anim = World.GetComponent<AnimationStateComponent>(Entity);

            _animator.SetFloat(_speedId,   anim.SpeedBlend);
            _animator.SetBool(_groundedId, anim.IsGrounded);
            _animator.SetBool(_freeFallId, !anim.IsGrounded && !anim.TriggerJump);

            if (anim.TriggerJump)
                _animator.SetTrigger(_jumpId);
        }
    }
}
