using System;

namespace HelloDev.Entities
{
    /// <summary>Applies one ECS component to an entity at spawn time.</summary>
    public interface IComponentInitializer
    {
        Type ComponentType { get; }
        void Apply(EcsWorld world, Entity entity);
    }
}
