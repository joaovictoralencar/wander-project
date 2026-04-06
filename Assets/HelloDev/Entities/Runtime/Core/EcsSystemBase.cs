using System;
using System.Collections.Generic;

namespace HelloDev.Entities
{
    /// <summary>
    /// Base class for ECS systems. Provides default no-op implementations and
    /// <c>[Serializable]</c> for inspector registration on <see cref="EcsSystemRunner"/>.
    /// </summary>
    [Serializable]
    public abstract class EcsSystemBase : IEcsSystem
    {
        /// <summary>
        /// Component types this system requires on an entity to process it.
        /// The runner builds the filter mask from these automatically.
        /// </summary>
        public virtual Type[] RequiredComponents => Array.Empty<Type>();

        // Sealed — the mask is an implementation detail. Override RequiredComponents instead.
        public int GetRequiredMask() => ComponentRegistry.BuildMask(RequiredComponents);

        public abstract void Initialize(EcsWorld world);

        public virtual void FixedExecute(EcsWorld world, List<int> entities, float fixedDeltaTime) { }
        public virtual void Execute(EcsWorld world, List<int> entities, float deltaTime) { }
        public virtual void Dispose() { }
    }
}
