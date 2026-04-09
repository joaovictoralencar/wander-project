using System;
using System.Collections.Generic;
using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Attack;
using Wander.Character.Components;
using Wander.Character.Events;
using Wander.Character.Systems;

namespace Wander.Player
{
    [RequiresSystem(typeof(AttackSystem))]
    [Provides(typeof(AttackComponent))]
    public class AttackBridge : EcsComponentBridge, IAttackAnimEventReceiver
    {
        [Header("Combo Data")]
        [SerializeField] private ComboDefinition[] _combos;

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _hitboxCollider;

        [Header("Animator Override")]
        [Tooltip("Name of the placeholder state in the Animator Controller")]
        [SerializeField] private string _attackStateA = "AttackA";
        [SerializeField] private string _attackStateB = "AttackB";
        [SerializeField] private float  _crossFadeDuration = 0.1f;

        private AnimatorOverrideController _overrideController;
        private bool _useStateA = true;

        // Input history for combo matching
        private readonly List<AttackInputType> _inputHistory = new();

        private IDisposable _stepStartedSub;
        private IDisposable _attackEndedSub;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_hitboxCollider != null)
            {
                _hitboxCollider.isTrigger = true;
                _hitboxCollider.enabled = false;
            }

            // Create override controller from the animator's current controller
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
                _animator.runtimeAnimatorController = _overrideController;
            }
        }

        protected override void OnInitialize()
        {
            Add(new AttackComponent());

            _stepStartedSub = World.Subscribe<AttackStepStartedEvent>(OnStepStarted);
            _attackEndedSub = World.Subscribe<AttackEndedEvent>(OnAttackEnded);
        }

        // ── Animation Event Callbacks (from AttackAnimEventProxy) ──

        public void OnComboWindowOpen()
        {
            var attack = Get<AttackComponent>();
            if (!attack.IsAttacking) return;
            attack.ComboWindowOpen = true;
            Set(attack);
        }

        public void OnComboWindowClose()
        {
            var attack = Get<AttackComponent>();
            attack.ComboWindowOpen = false;
            Set(attack);
        }

        public void OnHitboxActivate()
        {
            var attack = Get<AttackComponent>();
            if (!attack.IsAttacking) return;
            attack.HitboxActive = true;
            attack.HitLanded = false;
            Set(attack);

            if (_hitboxCollider != null)
                _hitboxCollider.enabled = true;
        }

        public void OnHitboxDeactivate()
        {
            var attack = Get<AttackComponent>();
            attack.HitboxActive = false;
            Set(attack);

            if (_hitboxCollider != null)
                _hitboxCollider.enabled = false;
        }

        // ── ECS Event Handlers ──

        private void OnStepStarted(AttackStepStartedEvent e)
        {
            if (e.Entity != Entity) return;

            var attack = Get<AttackComponent>();

            // Resolve combo if this is the first step (ComboIndex == -1)
            int comboIndex = e.ComboIndex;
            if (comboIndex < 0)
            {
                _inputHistory.Clear();
                comboIndex = ResolveCombo(attack.ComboInputCount);
                attack.CurrentComboIndex = comboIndex;
            }

            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                EndAttack();
                return;
            }

            var combo = _combos[comboIndex];
            int stepIndex = e.StepIndex;
            if (stepIndex >= combo.Steps.Length)
            {
                EndAttack();
                return;
            }

            var step = combo.Steps[stepIndex];

            // Copy per-step data into the component
            attack.StepDuration = step.Clip != null ? step.Clip.length : 0.5f;
            attack.StepDamageMultiplier = step.DamageMultiplier;
            Set(attack);

            PlayClip(step.Clip);
        }

        private void OnAttackEnded(AttackEndedEvent e)
        {
            if (e.Entity != Entity) return;
            EndAttack();
        }

        private void EndAttack()
        {
            _inputHistory.Clear();
            if (_hitboxCollider != null)
                _hitboxCollider.enabled = false;
        }

        // ── Combo Matching ──

        public void TrackInput(AttackInputType inputType)
        {
            if (inputType != AttackInputType.None)
                _inputHistory.Add(inputType);
        }

        private int ResolveCombo(int inputCount)
        {
            int bestIndex = -1;
            int bestLength = 0;

            for (int c = 0; c < _combos.Length; c++)
            {
                var pattern = _combos[c].InputPattern;
                if (pattern == null || pattern.Length == 0) continue;
                if (_inputHistory.Count > pattern.Length) continue;

                bool matches = true;
                for (int j = 0; j < _inputHistory.Count && j < pattern.Length; j++)
                {
                    if (_inputHistory[j] != pattern[j])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches && pattern.Length > bestLength)
                {
                    bestIndex = c;
                    bestLength = pattern.Length;
                }
            }

            return bestIndex;
        }

        // ── Animation Playback ──

        private void PlayClip(AnimationClip clip)
        {
            if (_overrideController == null || clip == null) return;

            string stateName = _useStateA ? _attackStateA : _attackStateB;
            _overrideController[stateName] = clip;
            _animator.CrossFadeInFixedTime(stateName, _crossFadeDuration);
            _useStateA = !_useStateA;
        }

        // ── Push / Pull ──

        protected override void OnPushToEcs()
        {
            var input = Get<MoveInputComponent>();
            TrackInput(input.AttackInput);
        }

        protected override void OnFixedPullFromEcs() { }

        // ── Hitbox Collision ──

        private void OnTriggerEnter(Collider other)
        {
            var attack = Get<AttackComponent>();
            if (!attack.HitboxActive || attack.HitLanded) return;

            var targetRoot = other.GetComponentInParent<EcsEntityRoot>();
            if (targetRoot == null || targetRoot.Entity == Entity) return;

            attack.HitLanded = true;
            Set(attack);

            var stats = Get<CombatStatsComponent>();
            float finalDamage = (stats.BaseDamage + stats.BonusDamage)
                              * attack.StepDamageMultiplier;

            World.Send(new HitEvent
            {
                Attacker = Entity,
                Target   = targetRoot.Entity,
                Damage   = finalDamage,
            });
        }

        protected override void OnDestroy()
        {
            _stepStartedSub?.Dispose();
            _attackEndedSub?.Dispose();
            base.OnDestroy();
        }
    }
}
