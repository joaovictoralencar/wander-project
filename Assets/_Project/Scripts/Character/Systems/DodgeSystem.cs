using System;
using System.Collections.Generic;
using HelloDev.Entities;
using Unity.Mathematics;
using Wander;
using Wander.Character.Components;
using Wander.Character.Events;

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

        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(DodgeComponent),
        };

        public override void Initialize(EcsWorld world) { }

        public override void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity   = world.GetEntity(entities[i]);
                var input    = world.GetComponent<MoveInputComponent>(entity);
                var dodge    = world.GetComponent<DodgeComponent>(entity);

                if (dodge.IsDodging)
                {
                    dodge.ElapsedTime += fixedDeltaTime;

                    if (dodge.ElapsedTime >= dodge.DodgeDuration)
                    {
                        dodge.IsDodging    = false;
                        dodge.ElapsedTime  = dodge.DodgeDuration;
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

                if (!input.Dodge || dodge.CooldownRemaining > 0f)
                    continue;

                // Need grounded check — read from MovementStateComponent if present
                if (world.TryGetComponent<MovementStateComponent>(entity, out var moveState) && !moveState.IsGrounded)
                    continue;

                // Block dodge entirely while attacking
                if (world.TryGetComponent<AttackComponent>(entity, out var attack) && attack.IsAttacking)
                    continue;

                input.Dodge = false;

                float3 dir = math.lengthsq(input.Direction) > 0.001f
                    ? math.normalize(input.Direction)
                    : float3.zero;

                dodge.IsDodging    = true;
                dodge.ElapsedTime  = 0f;
                dodge.Direction    = dir;

                world.SetComponent(entity, input);
                world.SetComponent(entity, dodge);
                world.Send(new DodgeStartedEvent { Entity = entity, Direction = dir });
                EcsDebug.Log($"Dodge started → Entity({entity.Id}) dir={dir}");
            }
        }
    }
}
