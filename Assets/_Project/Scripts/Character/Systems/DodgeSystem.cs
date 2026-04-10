using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Unity.Mathematics;
using Wander.Character.Attack;
using Wander.Character.Components;
using Wander.Character.Events;
using Wander.Components;

namespace Wander.Character.Systems
{
    /// <summary>
    /// Manages the dodge state machine each physics step.
    /// Fires <see cref="DodgeStartedEvent"/> and <see cref="DodgeEndedEvent"/> via the world event bus
    /// so any system or bridge can react without coupling.
    /// </summary>
    [Serializable]
    public class DodgeSystem : EcsSystemBase
    {
        public override int Order => 0;

        float dodgeBufferTime;
        bool wantsToDodge;

        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(DodgeComponent),
        };

        public override void Initialize(EcsWorld world)
        {
        }

        public override void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = world.GetEntity(entities[i]);
                var input = world.GetComponent<MoveInputComponent>(entity);
                var dodge = world.GetComponent<DodgeComponent>(entity);
                var transform = world.GetComponent<TransformSnapshotComponent>(entity);

                if (dodge.IsDodging)
                {
                    dodge.ElapsedTime += fixedDeltaTime;

                    if (dodge.ElapsedTime >= dodge.DodgeDuration)
                    {
                        dodge.IsDodging = false;
                        dodge.ElapsedTime = dodge.DodgeDuration;
                        dodge.CooldownRemaining = dodge.Cooldown;
                        world.SetComponent(entity, dodge);
                        world.Send(new DodgeEndedEvent { Entity = entity });
                        EcsDebug.Log($"Dodge ended → Entity({entity.Id})");
                        continue;
                    }
                    world.SetComponent(entity, dodge);
                    continue;
                }

                if (dodge.CooldownRemaining > 0f)
                {
                    dodge.CooldownRemaining = math.max(0f, dodge.CooldownRemaining - fixedDeltaTime);
                    world.SetComponent(entity, dodge);
                }

                if (dodge.CooldownRemaining > 0f)
                    continue;

                //Player is not pressing dodge
                if (!input.Dodge)
                {
                    if (wantsToDodge)
                    {
                        dodgeBufferTime += fixedDeltaTime;
                    }
                    else continue;
                }

                //Player pressed the dodge button
                if (!wantsToDodge)
                {
                    wantsToDodge = true; //player wants to dodge
                }
                else
                {
                    //Player has buffered the dodge input for too long
                    if (dodgeBufferTime > dodge.DodgeMaxBufferTime)
                    {
                        wantsToDodge = false;
                        dodgeBufferTime = 0f;
                        continue;
                    }

                    EcsDebug.Log($"<color=yellow>COYOTE</color> Dodge buffered! Entity({entity.Id})");
                }

                // Need grounded check — read from MovementStateComponent if present
                if (world.TryGetComponent<MovementStateComponent>(entity, out var moveState) && !moveState.IsGrounded)
                    continue;

                // Block dodge entirely while attacking
                if (world.TryGetComponent<AttackComponent>(entity, out var attack) && attack.IsAttacking)
                    continue;

                //Dodge logic bellow
                dodgeBufferTime = 0f;
                if (wantsToDodge && !input.Dodge)
                {
                    EcsDebug.Log($"<color=red>COYOTE</color> Dodge started! Entity({entity.Id})");
                }

                input.Dodge = false;
                wantsToDodge = false;

                float3 dir;
                if (math.lengthsq(input.Direction) > 0.001f)
                {
                    dir = math.normalize(input.Direction);
                }
                else if (world.TryGetComponent<TransformSnapshotComponent>(entity, out var transformSnapshot))
                {
                    dir = math.normalize(transformSnapshot.Forward);
                }
                else
                {
                    dir = new float3(0f, 0f, 1f);
                }

                dodge.IsDodging = true;
                dodge.ElapsedTime = 0f;
                dodge.Direction = dir;

                world.SetComponent(entity, input);
                world.SetComponent(entity, dodge);

                // Clear any lingering attack recovery so it doesn't block a new combo after the dodge
                if (world.TryGetComponent<AttackComponent>(entity, out var attackForClear) && attackForClear.IsRecovering)
                {
                    attackForClear.IsRecovering = false;
                    attackForClear.BufferedInput = AttackInputType.None;
                    world.SetComponent(entity, attackForClear);
                }

                world.Send(new DodgeStartedEvent { Entity = entity, Direction = dir });
                EcsDebug.Log($"Dodge started → Entity({entity.Id}) dir={dir}");
            }
        }
    }
}