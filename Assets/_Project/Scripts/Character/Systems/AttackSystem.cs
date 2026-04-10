using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Wander.Character.Attack;
using Wander.Character.Components;
using Wander.Character.Events;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Combo state machine running each physics step.
    /// Reads AttackComponent + MoveInputComponent, writes state,
    /// fires AttackStepStartedEvent / AttackEndedEvent.
    /// Window flags (combo, hitbox) are set externally by Animation Events.
    /// </summary>
    [Serializable]
    public class AttackSystem : EcsSystemBase
    {
        public override int Order => 5;

        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(AttackComponent),
        };

        public override void Initialize(EcsWorld world) { }

        public override void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity    = world.GetEntity(entities[i]);
                var input     = world.GetComponent<MoveInputComponent>(entity);
                var attack    = world.GetComponent<AttackComponent>(entity);

                // ── Buffer incoming input ──
                if (input.AttackInput != AttackInputType.None)
                {
                    attack.BufferedInput = input.AttackInput;
                    input.AttackInput = AttackInputType.None;
                    world.SetComponent(entity, input);
                }

                if (attack.IsAttacking)
                {
                    // ── Advance time ──
                    attack.ElapsedTime += fixedDeltaTime;

                    // Reset hit-landed when hitbox deactivates (for next step)
                    if (!attack.HitboxActive)
                        attack.HitLanded = false;

                    // ── Combo advance: input buffered while combo window is open ──
                    if (attack.ComboWindowOpen && attack.BufferedInput != AttackInputType.None)
                    {
                        int nextStep = attack.CurrentStepIndex + 1;

                        // Bounds check — don't advance past the last step
                        if (nextStep >= attack.MaxSteps)
                        {
                            attack.BufferedInput = AttackInputType.None;
                            attack.ComboWindowOpen = false;
                            world.SetComponent(entity, attack);
                            continue;
                        }

                        EcsDebug.Log($"[AttackSystem] COMBO ADVANCE step {attack.CurrentStepIndex}→{nextStep} | buffered={attack.BufferedInput}");
                        attack.ComboInputCount++;
                        attack.BufferedInput    = AttackInputType.None;
                        attack.CurrentStepIndex = nextStep;
                        attack.ElapsedTime      = 0f;
                        attack.HitboxActive     = false;
                        attack.ComboWindowOpen  = false;
                        attack.HitLanded        = false;

                        world.SetComponent(entity, attack);
                        world.Send(new AttackStepStartedEvent
                        {
                            Entity     = entity,
                            ComboIndex = attack.CurrentComboIndex,
                            StepIndex  = nextStep,
                        });
                        continue;
                    }

                    // ── Step finished (elapsed >= duration, no combo input) → end attack ──
                    if (attack.ElapsedTime >= attack.StepDuration)
                    {
                        attack.IsAttacking      = false;
                        attack.HitboxActive     = false;
                        attack.ComboWindowOpen  = false;
                        attack.BufferedInput    = AttackInputType.None;

                        world.SetComponent(entity, attack);
                        world.Send(new AttackEndedEvent { Entity = entity });
                        continue;
                    }

                    world.SetComponent(entity, attack);
                    continue;
                }

                // ── Not attacking — start new combo if input buffered and grounded ──
                if (attack.BufferedInput != AttackInputType.None)
                {
                    bool isGrounded = !world.TryGetComponent<MovementStateComponent>(entity, out var moveState) || moveState.IsGrounded;

                    if (isGrounded)
                    {
                        attack.IsAttacking      = true;
                        attack.CurrentStepIndex = 0;
                        attack.ComboInputCount  = 1;
                        attack.ElapsedTime      = 0f;
                        attack.HitboxActive     = false;
                        attack.ComboWindowOpen  = false;
                        attack.HitLanded        = false;

                        attack.BufferedInput = AttackInputType.None;

                        world.SetComponent(entity, attack);
                        world.Send(new AttackComboStartEvent { Entity = entity });
                    }
                    else
                    {
                        // Clear stale buffered input when not grounded
                        attack.BufferedInput = AttackInputType.None;
                        world.SetComponent(entity, attack);
                    }
                }
            }
        }
    }
}
