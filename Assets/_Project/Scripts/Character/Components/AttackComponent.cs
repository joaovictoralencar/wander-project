using System;
using UnityEngine;
using Wander.Character.Attack;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AttackComponent
    {
        // ── Config (tunable in Inspector) ──
        [Tooltip("Fraction of the last step's clip length used as cooldown before a new combo can start (0 = no recovery)")]
        public float RecoveryFraction;

        // ── Runtime state (written by AttackSystem + AttackBridge) ──

        [NonSerialized] public bool  IsAttacking;
        [NonSerialized] public int   CurrentComboIndex;   // index into bridge's ComboDefinition[]
        [NonSerialized] public int   CurrentStepIndex;    // step within the combo
        [NonSerialized] public float ElapsedTime;         // seconds since current step started
        [NonSerialized] public int   ComboInputCount;     // how many inputs registered so far in this combo

        // ── Per-step data (copied from ComboStep by bridge when step starts) ──

        [NonSerialized] public float StepDuration;          // clip.length
        [NonSerialized] public float StepDamageMultiplier;  // from ComboStep.DamageMultiplier
        [NonSerialized] public int   MaxSteps;              // total steps in resolved combo

        // ── Flags (set by Animation Events via proxy → bridge) ──

        [NonSerialized] public bool  ComboWindowOpen;
        [NonSerialized] public bool  HitboxActive;
        [NonSerialized] public bool  HitLanded;           // prevents multi-hit per step

        // ── Input buffer ──

        [NonSerialized] public AttackInputType BufferedInput;

        // ── Recovery state (post-combo cooldown) ──

        [NonSerialized] public bool  IsRecovering;
        [NonSerialized] public float RecoveryDuration;
        [NonSerialized] public float RecoveryElapsed;
    }
}