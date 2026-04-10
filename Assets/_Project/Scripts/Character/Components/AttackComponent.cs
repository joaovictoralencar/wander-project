using System;
using Wander.Character.Attack;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AttackComponent
    {
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
    }
}