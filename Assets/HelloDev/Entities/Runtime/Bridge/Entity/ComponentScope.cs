using System;

namespace HelloDev.Entities
{
    /// <summary>
    /// Scoped component modifier. Reads the component on creation, writes it back on Dispose.
    /// Use with <c>using</c> to ensure the write-back happens automatically:
    /// <code>
    /// using var scope = Modify&lt;AttackComponent&gt;();
    /// scope.Value.IsAttacking = true;
    /// // Automatically written back when scope is disposed
    /// </code>
    /// </summary>
    public sealed class ComponentScope<T> : IDisposable where T : unmanaged
    {
        private readonly EcsWorld _world;
        private readonly Entity _entity;

        public T Value;

        internal ComponentScope(EcsWorld world, Entity entity)
        {
            _world = world;
            _entity = entity;
            Value = world.GetComponent<T>(entity);
        }

        public void Dispose() => _world.SetComponent(_entity, Value);
    }
}
