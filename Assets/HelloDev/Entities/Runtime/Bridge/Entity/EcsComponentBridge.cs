using System;
using UnityEngine;

namespace HelloDev.Entities
{
    /// <summary>
    /// Base class for per-concern bridges. Receives <see cref="Entity"/> and <see cref="World"/>
    /// from <see cref="EcsEntityRoot"/> and participates in the push/pull cycle each Fixed/Update.
    /// </summary>
    public abstract class EcsComponentBridge : MonoBehaviour, IBridge
    {
        protected Entity Entity { get; private set; }
        protected EcsWorld World { get; private set; }

        /// <summary>
        /// System types this bridge depends on. Auto-registered at runtime when the bridge initializes —
        /// no need to add them manually to the <see cref="EcsEntityRoot"/> Systems list.
        /// </summary>
        public virtual Type[] RequiredSystems => Array.Empty<Type>();

        /// <summary>
        /// Component types this bridge adds in <see cref="OnInitialize"/>.
        /// Used by <see cref="EcsEntityRoot"/> editor validation.
        /// </summary>
        public virtual Type[] ProvidedComponents => Array.Empty<Type>();

        /// <summary>Stores entity/world references, auto-registers required systems, and calls <see cref="OnInitialize"/>.</summary>
        public void Initialize(IEntityContext context)
        {
            Entity = context.Entity;
            World  = context.World;
            AutoRegisterSystems();
            OnInitialize();
            EcsDebug.Log($"Bridge '{GetType().Name}' initialized → Entity({Entity.Id})");
        }

        /// <summary>Convenience overload for manual spawning without an <see cref="IEntityContext"/>.</summary>
        public void Initialize(EcsWorld world, Entity entity)
        {
            Entity = entity;
            World  = world;
            AutoRegisterSystems();
            OnInitialize();
            EcsDebug.Log($"Bridge '{GetType().Name}' initialized → Entity({Entity.Id})");
        }

        private void AutoRegisterSystems()
        {
            var runner = EcsSystemRunner.Instance;
            if (runner == null) return;
            foreach (var type in RequiredSystems)
                if (Activator.CreateInstance(type) is IEcsSystem system)
                    runner.AddSystem(system);
        }

        /// <summary>Override to add initial ECS components. Called once after <see cref="Initialize"/>.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>Override to push Unity state into ECS each FixedUpdate before systems run.</summary>
        protected virtual void OnPushToEcs() { }

        /// <summary>Override to apply ECS results to Unity physics each FixedUpdate after systems run (e.g. CharacterController.Move).</summary>
        protected virtual void OnFixedPullFromEcs() { }

        /// <summary>Override to pull ECS state back into Unity each Update after systems run (visuals, animation).</summary>
        protected virtual void OnPullFromEcs() { }

        public void PushToEcs()       => OnPushToEcs();
        public void FixedPullFromEcs() => OnFixedPullFromEcs();
        public void PullFromEcs()     => OnPullFromEcs();

        protected virtual void OnDestroy()
        {
            if (EcsSystemRunner.Instance != null)
                EcsSystemRunner.Instance.UnregisterBridge(this);
        }
    }
}
