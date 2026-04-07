using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.Entities
{
    /// <summary>
    /// Base class for per-concern bridges. Receives <see cref="Entity"/> and <see cref="World"/>
    /// from <see cref="EcsEntityRoot"/> and participates in the push/pull cycle each Fixed/Update.
    /// <para>
    /// Use <c>[RequiresSystem(typeof(...))]</c> to declare system dependencies and
    /// <c>[Provides(typeof(...))]</c> to declare owned components (used for editor validation).
    /// </para>
    /// </summary>
    public abstract class EcsComponentBridge : MonoBehaviour
    {
        protected Entity Entity { get; private set; }
        protected EcsWorld World { get; private set; }

        /// <summary>Stores entity/world references, auto-registers required systems, and calls <see cref="OnInitialize"/>.</summary>
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
            foreach (var attr in GetType().GetCustomAttributes(typeof(RequiresSystemAttribute), true))
            {
                var reqAttr = (RequiresSystemAttribute)attr;
                foreach (var type in reqAttr.SystemTypes)
                    if (Activator.CreateInstance(type) is IEcsSystem system)
                        runner.AddSystem(system);
            }
        }

        /// <summary>Override to add initial ECS components via <see cref="Add{T}"/>. Called once after <see cref="Initialize"/>.</summary>
        protected virtual void OnInitialize() { }

        #region Component Helpers

        /// <summary>Registers an ECS component this bridge owns. Uses set-or-add for safety.</summary>
        protected void Add<T>(T value) where T : unmanaged => World.SetOrAddComponent(Entity, value);

        /// <summary>Reads the current value of a component from the entity.</summary>
        protected T Get<T>() where T : unmanaged => World.GetComponent<T>(Entity);

        /// <summary>Updates an existing component on the entity.</summary>
        protected void Set<T>(T value) where T : unmanaged => World.SetComponent(Entity, value);

        /// <summary>Returns true if the entity currently has the given component.</summary>
        protected bool Has<T>() where T : unmanaged => World.HasComponent<T>(Entity);

        #endregion

        #region Attribute Reflection Helpers

        /// <summary>Reads <see cref="ProvidesAttribute"/> from this bridge's type. Cached per-type by callers.</summary>
        internal static Type[] GetProvidedComponentTypes(Type bridgeType)
        {
            var result = new List<Type>();
            foreach (var attr in bridgeType.GetCustomAttributes(typeof(ProvidesAttribute), true))
                result.AddRange(((ProvidesAttribute)attr).ComponentTypes);
            return result.ToArray();
        }

        /// <summary>Reads <see cref="RequiresSystemAttribute"/> from this bridge's type.</summary>
        internal static Type[] GetRequiredSystemTypes(Type bridgeType)
        {
            var result = new List<Type>();
            foreach (var attr in bridgeType.GetCustomAttributes(typeof(RequiresSystemAttribute), true))
                result.AddRange(((RequiresSystemAttribute)attr).SystemTypes);
            return result.ToArray();
        }

        #endregion

#if UNITY_EDITOR && ODIN_INSPECTOR
        [BoxGroup("ECS Info"), ShowInInspector, ReadOnly, LabelText("Provides"), PropertyOrder(100)]
        private string _providedInfo
        {
            get
            {
                var types = GetProvidedComponentTypes(GetType());
                return types.Length > 0 ? string.Join(", ", Array.ConvertAll(types, t => t.Name)) : "(adapter only)";
            }
        }

        [BoxGroup("ECS Info"), ShowInInspector, ReadOnly, LabelText("Systems"), PropertyOrder(101)]
        private string _systemsInfo
        {
            get
            {
                var types = GetRequiredSystemTypes(GetType());
                return types.Length > 0 ? string.Join(", ", Array.ConvertAll(types, t => t.Name)) : "(none)";
            }
        }
#endif

        /// <summary>Override to push Unity state into ECS each FixedUpdate before systems run.</summary>
        protected virtual void OnPushToEcs() { }

        /// <summary>Override to apply ECS results to Unity physics each FixedUpdate after systems run.</summary>
        protected virtual void OnFixedPullFromEcs() { }

        /// <summary>Override to pull ECS state back into Unity each Update after systems run.</summary>
        protected virtual void OnPullFromEcs() { }

        internal void PushToEcs()       => OnPushToEcs();
        internal void FixedPullFromEcs() => OnFixedPullFromEcs();
        internal void PullFromEcs()     => OnPullFromEcs();

        protected virtual void OnDestroy()
        {
            if (EcsSystemRunner.Instance != null)
                EcsSystemRunner.Instance.UnregisterBridge(this);
        }
    }
}
