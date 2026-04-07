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
    /// Fires <see cref="DodgeStartedEvent"/> and <see cref="DodgeEndedEvent"/> via the world event queue
    /// so any system or bridge can react without coupling.
    /// </summary>
    [Serializable]
    public class DodgeSystem : EcsSystemBase
    {
        public override int Order => 0;

        public override Type[] RequiredComponents => new[]
        {
            typeof(MoveInputComponent),
            typeof(MovementStateComponent),
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
                var moveState = world.GetComponent<MovementStateComponent>(entity);

                if (dodge.IsDodging)
                {
                    dodge.ElapsedTime += fixedDeltaTime;

                    if (dodge.ElapsedTime >= dodge.DodgeDuration)
                    {
                        dodge.IsDodging    = false;
                        dodge.ElapsedTime  = dodge.DodgeDuration;
                        moveState.CanMove  = true;
                        world.SetComponent(entity, dodge);
                        world.SetComponent(entity, moveState);
                        world.EnqueueEvent(new DodgeEndedEvent { Entity = entity });
                        EcsDebug.Log($"Dodge ended → Entity({entity.Id})");
                        continue;
                    }

                    world.SetComponent(entity, dodge);
                    continue;
                }

                if (!input.Dodge)
                    continue;

                input.Dodge = false;

                float3 dir = math.lengthsq(input.Direction) > 0.001f
                    ? math.normalize(input.Direction)
                    : float3.zero;

                dodge.IsDodging    = true;
                dodge.ElapsedTime  = 0f;
                dodge.Direction    = dir;
                moveState.CanMove  = false;

                world.SetComponent(entity, input);
                world.SetComponent(entity, dodge);
                world.SetComponent(entity, moveState);
                world.EnqueueEvent(new DodgeStartedEvent { Entity = entity, Direction = dir });
                EcsDebug.Log($"Dodge started → Entity({entity.Id}) dir={dir}");
            }
        }
    }
}
