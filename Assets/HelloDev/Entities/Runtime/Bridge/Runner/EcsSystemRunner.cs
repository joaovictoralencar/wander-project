using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Entities
{
    // Runs at -50 so any bridge that needs to apply results (e.g. CharacterController.Move)
    // can safely do so in its own FixedUpdate at default order 0.
    [DefaultExecutionOrder(-50)]
    public class EcsSystemRunner : MonoBehaviour
    {
        public static EcsSystemRunner Instance { get; private set; }

        [SerializeField] private EcsConfigAsset _config;
        
        private EcsWorld _world;
        private readonly List<IEcsSystem> _systems = new();
        private readonly List<IBridge> _bridges = new();
        private readonly EcsCommandBuffer _commandBuffer = new();

        public EcsWorld World => _world;

        /// <summary>Read-only view of all currently registered systems (runtime only).</summary>
        public IReadOnlyList<IEcsSystem> Systems => _systems;

        /// <summary>Returns true if a system of type <typeparamref name="T"/> is already registered.</summary>
        public bool HasSystem<T>() where T : IEcsSystem
        {
            for (int i = 0; i < _systems.Count; i++)
                if (_systems[i] is T) return true;
            return false;
        }

        [Header("Debug")]
        [SerializeField] private bool _debugLogs   = true;
        [SerializeField] private bool _verboseLogs = false;

        private void Awake()
        {
            Instance = this;

            EcsDebug.Enabled = _debugLogs;
            EcsDebug.Verbose = _verboseLogs;

            if (_config != null)
                EcsRuntime.Initialize(_config);
            else
                Debug.LogWarning("[ECS] No EcsConfigAsset assigned to EcsSystemRunner. Using default MaxEntities (128).");

            ComponentRegistry.Reset();
            _world = new EcsWorld();

            EcsDebug.Log("World created.");
        }

        private void Start()
        {
            if (!_debugLogs) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[ECS] Runner ready — {_systems.Count} system(s), {_bridges.Count} bridge(s)");
            foreach (var s in _systems) sb.AppendLine($"  + {s.GetType().Name}");
            foreach (var b in _bridges) sb.AppendLine($"  ~ {(b as UnityEngine.Object)?.name ?? b.GetType().Name}");
            Debug.Log(sb.ToString().TrimEnd());
        }

        /// <summary>
        /// Registers a system and calls its <see cref="IEcsSystem.Initialize"/>.
        /// Safe to call from any MonoBehaviour Start() — the world is guaranteed ready by then.
        /// Duplicate system types are silently ignored.
        /// </summary>
        public void AddSystem(IEcsSystem system)
        {
            foreach (var existing in _systems)
                if (existing.GetType() == system.GetType()) return;

            _systems.Add(system);
            system.Initialize(_world);
            EcsDebug.Log($"System registered: {system.GetType().Name}");
        }

        public void RegisterBridge(IBridge bridge)
        {
            if (_bridges.Contains(bridge)) return;
            _bridges.Add(bridge);
            EcsDebug.Log($"Bridge registered: {(bridge as UnityEngine.Object)?.name ?? bridge.GetType().Name} ({bridge.GetType().Name})");
        }

        public void UnregisterBridge(IBridge bridge)
        {
            if (_bridges.Contains(bridge)) _bridges.Remove(bridge);
        }

        private void FixedUpdate()
        {
            foreach (var bridge in _bridges) bridge.PushToEcs();

            foreach (var system in _systems)
            {
                var entities = _world.GetEntitiesWithMask(system.GetRequiredMask());
                if (EcsDebug.Verbose && entities.Count > 0)
                    Debug.Log($"[ECS] FixedUpdate — {system.GetType().Name}: {entities.Count} entity/entities");
                system.FixedExecute(_world, entities, Time.fixedDeltaTime);
            }

            _commandBuffer.Flush(_world);

            foreach (var bridge in _bridges) bridge.FixedPullFromEcs();
        }

        private void Update()
        {
            foreach (var system in _systems)
            {
                var entities = _world.GetEntitiesWithMask(system.GetRequiredMask());
                if (EcsDebug.Verbose && entities.Count > 0)
                    Debug.Log($"[ECS] Update — {system.GetType().Name}: {entities.Count} entity/entities");
                system.Execute(_world, entities, Time.deltaTime);
            }

            foreach (var bridge in _bridges) bridge.PullFromEcs();
        }

        private void OnDestroy()
        {
            // Null the instance first — bridges check this in their own OnDestroy to detect
            // world teardown and avoid accessing disposed NativeCollections.
            Instance = null;

            foreach (var system in _systems) system.Dispose();
            _world.Dispose();
        }
    }
}