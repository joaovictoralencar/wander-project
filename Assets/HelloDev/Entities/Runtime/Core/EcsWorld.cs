using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

namespace HelloDev.Entities
{
    /// <summary>
    /// EcsWorld is the main class that manages entities and their components.
    /// It provides methods to create entities, add/remove components, and query component data.
    /// It also handles the lifecycle of component storages, ensuring that unmanaged resources are properly disposed of when the world is destroyed.
    /// </summary>
    public class EcsWorld : IDisposable
    {
        private int _nextEntityId = 0;
        private readonly Queue<int> _freeIds = new();

        // One generation counter per slot — starts at 0, increments on every recycle.
        private readonly int[] _generations = new int[EcsRuntime.MaxEntities];

        private readonly Dictionary<Type, IComponentStorage> _storages = new();
        private readonly int _maxEntities = EcsRuntime.MaxEntities;

        public Entity CreateEntity()
        {
            var id = _freeIds.Count > 0 ? _freeIds.Dequeue() : _nextEntityId++;

            // The entity is born into whatever generation this slot is currently on.
            return new Entity(id, _generations[id]);
        }

        public void DestroyEntity(Entity entity)
        {
            // Validate first — refuse to destroy a stale or already-dead entity.
            if (!IsAlive(entity))
            {
                Debug.LogWarning($"Tried to destroy entity {entity.Id} but it's already dead.");
                return;
            }

            // Increment the generation — any old references to this ID are now stale.
            _generations[entity.Id]++;

            // Clear the signature so this slot no longer matches any query.
            _signatures[entity.Id] = 0;

            // Mark all components as absent — the data stays in the array
            // but will be overwritten when this slot is reused.
            foreach (var storage in _storages.Values)
                storage.RemoveComponent(entity.Id);

            _freeIds.Enqueue(entity.Id);

            // Purge any cached filter lists that included this entity.
            InvalidateCacheFor(entity.Id);
        }

        // Every operation on the world should call this guard first.
        public bool IsAlive(Entity entity) => entity.Id >= 0 && _generations[entity.Id] == entity.Generation;

        // Lazily creates storage for a component type the first time it's needed.
        private ComponentStorage<T> GetOrCreateComponentStorage<T>() where T : unmanaged
        {
            var type = typeof(T);
            if (!_storages.TryGetValue(type, out var storage))
            {
                storage = new ComponentStorage<T>(_maxEntities);
                _storages[type] = storage;
            }

            return (ComponentStorage<T>)storage;
        }

        #region Component Registry

        public readonly ComponentRegistry Registry = new();

        // The current component signature for each entity slot.
        private readonly int[] _signatures = new int[EcsRuntime.MaxEntities];
        private readonly Dictionary<int, List<int>> _filterCache = new();

        // Systems call this to get their pre-filtered entity list.
        public List<int> GetEntitiesWithMask(int requiredMask)
        {
            if (!_filterCache.TryGetValue(requiredMask, out var list))
            {
                // Cache miss — build the list now and store it.
                list = new List<int>();
                for (int i = 0; i < _nextEntityId; i++)
                    if ((_signatures[i] & requiredMask) == requiredMask)
                        list.Add(i);

                _filterCache[requiredMask] = list;
            }

            return list;
        }

        private void InvalidateCacheFor(int entityId)
        {
            // Any cached list that included or excluded this entity may now be wrong.
            // Simplest correct approach: remove entries that could have changed.
            // For 100 entities and a handful of systems, a full clear is fine.
            _filterCache.Clear();
        }

        #endregion

        #region Management Methods

        public void AddComponent<T>(Entity entity, T component) where T : unmanaged
        {
            GetOrCreateComponentStorage<T>().Set(entity.Id, component);

            // Update this entity's signature and refresh any affected cache entries.
            int bit = Registry.GetOrRegister(typeof(T));
            _signatures[entity.Id] |= (1 << bit);
            InvalidateCacheFor(entity.Id);
        }

        public void RemoveComponent<T>(Entity entity) where T : unmanaged
        {
            GetOrCreateComponentStorage<T>().RemoveComponent(entity.Id);

            int bit = Registry.GetOrRegister(typeof(T));
            _signatures[entity.Id] &= ~(1 << bit); // clear the bit
            InvalidateCacheFor(entity.Id);
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged
        {
            return GetOrCreateComponentStorage<T>().HasComponent(entity.Id);
        }

        public T GetComponent<T>(Entity entity) where T : unmanaged
        {
            if (!IsAlive(entity))
            {
                Debug.LogWarning($"GetComponent<{typeof(T).Name}> called on dead entity {entity}. Returning default.");
                return default;
            }
            return GetOrCreateComponentStorage<T>().Get(entity.Id);
        }

        public void SetComponent<T>(Entity entity, T value) where T : unmanaged
        {
            // SetComponent updates data only — it does not add the component or update the signature.
            // Call AddComponent first if the entity does not yet have this component.
            Debug.Assert(HasComponent<T>(entity), $"SetComponent<{typeof(T).Name}> called on entity {entity} which does not have that component. Use AddComponent instead.");
            GetOrCreateComponentStorage<T>().Set(entity.Id, value);
        }

        // Reconstructs a valid Entity handle from a raw ID returned by GetEntitiesWithMask.
        public Entity GetEntity(int id) => new Entity(id, _generations[id]);

        // Exposes raw component data for job scheduling.
        // The NativeArray is indexed by entity ID — jobs must use [NativeDisableParallelForRestriction]
        // when writing, since they index by entity ID rather than job index.
        public NativeArray<T> GetComponentDataArray<T>() where T : unmanaged
            => GetOrCreateComponentStorage<T>().Data;

        #endregion

        public void Dispose()
        {
            foreach (IComponentStorage storage in _storages.Values)
            {
                storage.Dispose();
            }
        }

        public void AddComponentBoxed(Entity entity, Type componentType, object componentData)
        {
            var method = _addComponentMethodCache.GetOrAdd(componentType,
                t => typeof(EcsWorld)
                    .GetMethod(nameof(AddComponent), BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(t));

            method.Invoke(this, new[] { entity, componentData });
        }

        public void RemoveComponentBoxed(Entity entity, Type componentType)
        {
            var method = _removeComponentMethodCache.GetOrAdd(componentType,
                t => typeof(EcsWorld)
                    .GetMethod(nameof(RemoveComponent), BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(t));

            method.Invoke(this, new object[] { entity });
        }

        // Reflection cache — built once per component type, reused every flush.
        private readonly MethodCache _addComponentMethodCache = new();
        private readonly MethodCache _removeComponentMethodCache = new();

        private class MethodCache
        {
            private readonly Dictionary<Type, MethodInfo> _cache = new();

            public MethodInfo GetOrAdd(Type key, Func<Type, MethodInfo> factory)
            {
                if (!_cache.TryGetValue(key, out var method))
                    _cache[key] = method = factory(key);
                return method;
            }
        }
    }
}