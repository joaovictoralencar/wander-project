using UnityEngine;

namespace HelloDev.Entities
{
    /// <summary>
    /// Base class for systems that need access to Unity APIs (Animator, Collider, etc.).
    /// Lives on a GameObject alongside bridges, but participates in the system execution pipeline.
    /// <para>
    /// Use when behavior logic requires managed references that can't live in unmanaged components.
    /// Managed systems run after pure systems each FixedUpdate/Update, sorted by <see cref="Order"/>.
    /// </para>
    /// <para>
    /// Discovery: <see cref="EcsEntityRoot"/> auto-discovers managed systems on its hierarchy,
    /// same as bridges. Each instance receives its own <see cref="Entity"/> and <see cref="World"/>.
    /// </para>
    /// </summary>
    public abstract class EcsManagedSystem : MonoBehaviour
    {
        public Entity Entity { get; private set; }
        public EcsWorld World { get; private set; }

        /// <summary>
        /// Execution priority among managed systems. Lower values run first. Default is 0.
        /// </summary>
        public virtual int Order => 0;

        public void Initialize(EcsWorld world, Entity entity)
        {
            World = world;
            Entity = entity;
            OnInitialize();
            EcsDebug.Log($"ManagedSystem '{GetType().Name}' initialized → Entity({Entity.Id})");
        }

        /// <summary>Override to subscribe to events or perform one-time setup.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>Called each FixedUpdate after pure systems execute, before events flush.</summary>
        public virtual void ManagedFixedUpdate(float fixedDeltaTime) { }

        /// <summary>Called each Update after pure systems execute, before events flush.</summary>
        public virtual void ManagedUpdate(float deltaTime) { }

        /// <summary>Called when the runner is being destroyed.</summary>
        public virtual void ManagedDispose() { }

        #region Component Helpers

        protected T Get<T>() where T : unmanaged => World.GetComponent<T>(Entity);
        protected void Set<T>(T value) where T : unmanaged => World.SetComponent(Entity, value);
        protected void Add<T>(T value) where T : unmanaged => World.SetOrAddComponent(Entity, value);
        protected bool Has<T>() where T : unmanaged => World.HasComponent<T>(Entity);

        /// <summary>
        /// Returns a scoped modifier that reads the component now and writes it back on Dispose.
        /// Use with <c>using</c>: <c>using var scope = Modify&lt;T&gt;(); scope.Value.Field = x;</c>
        /// </summary>
        protected ComponentScope<T> Modify<T>() where T : unmanaged => new(World, Entity);

        #endregion

        protected virtual void OnDestroy()
        {
            if (EcsSystemRunner.Instance != null)
                EcsSystemRunner.Instance.UnregisterManagedSystem(this);
        }
    }
}
