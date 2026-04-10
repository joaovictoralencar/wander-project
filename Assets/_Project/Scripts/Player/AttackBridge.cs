using System;
using System.Collections.Generic;
using HelloDev.Entities;
using UnityEngine;
using UnityEngine.Events;
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
        private static readonly int SwordLayerActivated = Animator.StringToHash("SwordLayerActivated");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");

        [Header("Combo Data")] [SerializeField]
        private ComboDefinition[] _combos;

        [SerializeField] private AttackComponent _attack = new() { RecoveryFraction = 0.1f };

        [Header("References")] [SerializeField]
        private Animator _animator;

        [SerializeField] private Collider _hitboxCollider;

        [Header("Animator Override")] [SerializeField]
        private string _attackStateA = "AttackA";

        [SerializeField] private string _attackStateB = "AttackB";
        [SerializeField] private float _crossFadeDuration = 0.1f;

        private AnimatorOverrideController _overrideController;
        private AnimationClip _slotClipA;
        private AnimationClip _slotClipB;
        private bool _useStateA = true;

        // Input history for combo matching
        private readonly List<AttackInputType> _inputHistory = new();

        private IDisposable _comboStartSub;
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
                DiscoverAttackSlots();
            }
        }

        /// <summary>
        /// Auto-discovers the placeholder clips in the AttackA/AttackB states
        /// by scanning all layers. No layer name or clip references needed.
        /// </summary>
        private void DiscoverAttackSlots()
        {
            int stateHashA = Animator.StringToHash(_attackStateA);
            int stateHashB = Animator.StringToHash(_attackStateB);

            for (int layer = 0; layer < _animator.layerCount; layer++)
            {
                if (_slotClipA == null)
                {
                    _animator.Play(stateHashA, layer, 0f);
                    _animator.Update(0f);
                    var stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (stateInfo.shortNameHash == stateHashA)
                    {
                        var clipInfo = _animator.GetCurrentAnimatorClipInfo(layer);
                        if (clipInfo.Length > 0) _slotClipA = clipInfo[0].clip;
                    }
                }

                if (_slotClipB == null)
                {
                    _animator.Play(stateHashB, layer, 0f);
                    _animator.Update(0f);
                    var stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (stateInfo.shortNameHash == stateHashB)
                    {
                        var clipInfo = _animator.GetCurrentAnimatorClipInfo(layer);
                        if (clipInfo.Length > 0) _slotClipB = clipInfo[0].clip;
                    }
                }

                if (_slotClipA != null && _slotClipB != null) break;
            }

            _animator.Rebind();
            _animator.Update(0f);

            if (_slotClipA == null || _slotClipB == null)
                Debug.LogWarning("[AttackBridge] AttackA/AttackB states need placeholder clips for override to work.");
        }

        protected override void OnInitialize()
        {
            Add(_attack);

            _comboStartSub = World.Subscribe<AttackComboStartEvent>(OnComboStart);
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
            using var attack = Modify<AttackComponent>();
            attack.Value.ComboWindowOpen = false;
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
            using var attack = Modify<AttackComponent>();
            attack.Value.HitboxActive = false;

            if (_hitboxCollider != null)
                _hitboxCollider.enabled = false;
        }

        // ── ECS Event Handlers ──

        private void OnComboStart(AttackComboStartEvent e)
        {
            if (e.Entity != Entity) return;

            var attack = Get<AttackComponent>();
            _inputHistory.Clear();
            int comboIndex = ResolveCombo(attack.ComboInputCount);
            attack.CurrentComboIndex = comboIndex;

            if (comboIndex < 0 || comboIndex >= _combos.Length)
            {
                EndAttack();
                return;
            }

            var combo = _combos[comboIndex];
            if (combo.Steps.Length == 0)
            {
                EndAttack();
                return;
            }

            var step = combo.Steps[0];
            float rate = step.PlayRate > 0f ? step.PlayRate : 1f;
            attack.StepDuration = (step.Clip != null ? step.Clip.length : 0.5f) / rate;
            attack.StepDuration *= .9f; // added slightly delay to make sure the attack animation doesn't go until the end of the clip for steps
            attack.StepDamageMultiplier = step.DamageMultiplier;
            attack.MaxSteps = combo.Steps.Length;
            Set(attack);

            PlayClip(step.Clip, step.PlayRate);
        }

        private void OnStepStarted(AttackStepStartedEvent e)
        {
            if (e.Entity != Entity) return;

            var attack = Get<AttackComponent>();
            int comboIndex = e.ComboIndex;

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
            float rate = step.PlayRate > 0f ? step.PlayRate : 1f;
            attack.StepDuration = (step.Clip != null ? step.Clip.length : 0.5f) / rate;
            attack.StepDamageMultiplier = step.DamageMultiplier;
            Set(attack);

            PlayClip(step.Clip, step.PlayRate);
        }

        private void OnAttackEnded(AttackEndedEvent e)
        {
            if (e.Entity != Entity) return;
            EndAttack();
        }

        private void EndAttack()
        {
            _animator.SetBool(SwordLayerActivated, true);
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
        private void PlayClip(AnimationClip clip, float playRate = 1f)
        {
            if (_overrideController == null || clip == null) return;

            var slotClip = _useStateA ? _slotClipA : _slotClipB;
            var stateName = _useStateA ? _attackStateA : _attackStateB;

            if (slotClip == null) return;

            _overrideController[slotClip] = clip;
            _animator.Update(0f);
            _animator.SetFloat(AttackSpeed, playRate);
            _animator.CrossFadeInFixedTime(stateName, _crossFadeDuration);
            _useStateA = !_useStateA;
            _animator.SetBool("SwordLayerActivated", false);
        }

        // ── Push / Pull ──

        protected override void OnPushToEcs()
        {
            var input = Get<MoveInputComponent>();
            TrackInput(input.AttackInput);
        }

        protected override void OnFixedPullFromEcs()
        {
        }

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
                Target = targetRoot.Entity,
                Damage = finalDamage,
            });
        }

        protected override void OnDestroy()
        {
            _comboStartSub?.Dispose();
            _stepStartedSub?.Dispose();
            _attackEndedSub?.Dispose();
            base.OnDestroy();
        }
    }
}