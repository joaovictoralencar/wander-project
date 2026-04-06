using System;

namespace HelloDev.Entities
{
    /// <summary>
    /// Base class for zero-boilerplate component initializers. Subclass once per component;
    /// Unity serializes all struct fields via <see cref="Component"/> automatically.
    /// </summary>
    [Serializable]
    public abstract class ComponentInit<T> : IComponentInitializer where T : unmanaged
    {
        public T Component;

        public Type ComponentType => typeof(T);
        public void Apply(EcsWorld world, Entity entity) => world.AddComponent(entity, Component);
    }
}
