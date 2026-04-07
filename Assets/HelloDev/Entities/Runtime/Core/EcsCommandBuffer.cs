using System;
using System.Collections.Generic;

namespace HelloDev.Entities
{
    // A discriminated union describing one deferred operation.
    // Using an enum + struct keeps this allocation-free.
    public enum EcsCommandType
    {
        AddComponent,
        RemoveComponent,
        DestroyEntity
    }

    public struct EcsCommand
    {
        public EcsCommandType Type;
        public Entity Entity;
        public Type ComponentType;

        // We store component data as a boxed object here.
        // This is the one concession to managed memory — it's acceptable because
        // commands are processed outside of Jobs, during the flush phase.
        public object ComponentData;
    }

    public class EcsCommandBuffer
    {
        private readonly List<EcsCommand> _commands = new();

        public void AddComponent<T>(Entity entity, T component) where T : unmanaged
        {
            _commands.Add(new EcsCommand
            {
                Type = EcsCommandType.AddComponent,
                Entity = entity,
                ComponentType = typeof(T),
                ComponentData = component // boxed, but only lives until flush
            });
        }

        public void RemoveComponent<T>(Entity entity) where T : unmanaged
        {
            _commands.Add(new EcsCommand
            {
                Type = EcsCommandType.RemoveComponent,
                Entity = entity,
                ComponentType = typeof(T)
            });
        }

        public void DestroyEntity(Entity entity)
        {
            _commands.Add(new EcsCommand
            {
                Type = EcsCommandType.DestroyEntity,
                Entity = entity
            });
        }

        // Called by the system runner after all systems have executed.
        public void Flush(EcsWorld world)
        {
            try
            {
                foreach (var cmd in _commands)
                {
                    if (!world.IsAlive(cmd.Entity) && cmd.Type != EcsCommandType.DestroyEntity)
                        continue;

                    switch (cmd.Type)
                    {
                        case EcsCommandType.AddComponent:
                            world.AddComponentBoxed(cmd.Entity, cmd.ComponentType, cmd.ComponentData);
                            break;

                        case EcsCommandType.RemoveComponent:
                            world.RemoveComponentBoxed(cmd.Entity, cmd.ComponentType);
                            break;

                        case EcsCommandType.DestroyEntity:
                            world.DestroyEntity(cmd.Entity);
                            break;
                    }
                }
            }
            finally
            {
                _commands.Clear();
            }
        }
    }
}