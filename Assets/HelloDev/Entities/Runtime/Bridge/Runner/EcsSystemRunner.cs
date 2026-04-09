using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Entities
{
    [DefaultExecutionOrder(-50)]
    public class EcsSystemRunner : MonoBehaviour
    {
        public static EcsSystemRunner Instance { get; private set; }

        [Header("Config")]
        [Tooltip("Maximum number of entities the ECS world can hold. Set before play.")]
        [SerializeField] private int _maxEntities = 128;

        private EcsWorld _world;
        private readonly List<IEcsSystem> _systems = new();
        private readonly List<EcsComponentBridge> _bridges = new();
        private readonly List<EcsManagedSystem> _managedSystems = new();

        [Header("Global Systems")]
        [Tooltip("Systems that are always active regardless of entity composition. Use for event-only systems (e.g. DamageSystem).")]
        [SerializeReference]
        public List<EcsSystemBase> GlobalSystems = new();

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

            Debug.Assert(_maxEntities is > 0 and <= 1024, "MaxEntities must be between 1 and 1024.");
            EcsRuntime.MaxEntities = _maxEntities;

            ComponentRegistry.Reset();
            _world = new EcsWorld();

            for (int i = 0; i < GlobalSystems.Count; i++)
                if (GlobalSystems[i] != null)
                    AddSystem(GlobalSystems[i]);

            EcsDebug.Log("World created.");
        }

        private void Start()
        {
            if (!_debugLogs) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[ECS] Runner ready — {_systems.Count} system(s), {_managedSystems.Count} managed system(s), {_bridges.Count} bridge(s)");
            foreach (var s in _systems) sb.AppendLine($"  + {s.GetType().Name}");
            foreach (var ms in _managedSystems) sb.AppendLine($"  ▸ {ms.name} ({ms.GetType().Name})");
            foreach (var b in _bridges) sb.AppendLine($"  ~ {b.name} ({b.GetType().Name})");
            Debug.Log(sb.ToString().TrimEnd());
        }

        /// <summary>
        /// Registers a system and calls its <see cref="IEcsSystem.Initialize"/>.
        /// Duplicate system types are silently ignored.
        /// </summary>
        public void AddSystem(IEcsSystem system)
        {
            foreach (var existing in _systems)
                if (existing.GetType() == system.GetType()) return;

            _systems.Add(system);
            system.Initialize(_world);

            _systems.Sort((a, b) =>
            {
                int oa = a is EcsSystemBase sa ? sa.Order : 0;
                int ob = b is EcsSystemBase sb ? sb.Order : 0;
                return oa.CompareTo(ob);
            });

            EcsDebug.Log($"System registered: {system.GetType().Name} (order={((system is EcsSystemBase s) ? s.Order : 0)})");
        }

        internal void RegisterBridge(EcsComponentBridge bridge)
        {
            if (_bridges.Contains(bridge)) return;
            _bridges.Add(bridge);
            EcsDebug.Log($"Bridge registered: {bridge.name} ({bridge.GetType().Name})");
        }

        internal void UnregisterBridge(EcsComponentBridge bridge)
        {
            _bridges.Remove(bridge);
        }

        public void RegisterManagedSystem(EcsManagedSystem system)
        {
            if (_managedSystems.Contains(system)) return;
            _managedSystems.Add(system);
            _managedSystems.Sort((a, b) => a.Order.CompareTo(b.Order));
            EcsDebug.Log($"ManagedSystem registered: {system.GetType().Name} (order={system.Order})");
        }

        internal void UnregisterManagedSystem(EcsManagedSystem system)
        {
            _managedSystems.Remove(system);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _bridges.Count; i++) _bridges[i].PushToEcs();

            for (int i = 0; i < _systems.Count; i++)
            {
                var system = _systems[i];
                var entities = _world.GetEntitiesWithMask(system.GetRequiredMask());
                if (EcsDebug.Verbose && entities.Count > 0)
                    Debug.Log($"[ECS] FixedUpdate — {system.GetType().Name}: {entities.Count} entity/entities");
                system.FixedExecute(_world, entities, Time.fixedDeltaTime);
            }

            _world.FlushCommands();

            for (int i = 0; i < _managedSystems.Count; i++)
                _managedSystems[i].ManagedFixedUpdate(Time.fixedDeltaTime);

            _world.FlushEvents();

            for (int i = 0; i < _bridges.Count; i++) _bridges[i].FixedPullFromEcs();
        }

        private void Update()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                var system = _systems[i];
                var entities = _world.GetEntitiesWithMask(system.GetRequiredMask());
                if (EcsDebug.Verbose && entities.Count > 0)
                    Debug.Log($"[ECS] Update — {system.GetType().Name}: {entities.Count} entity/entities");
                system.Execute(_world, entities, Time.deltaTime);
            }

            for (int i = 0; i < _managedSystems.Count; i++)
                _managedSystems[i].ManagedUpdate(Time.deltaTime);

            _world.FlushEvents();

            for (int i = 0; i < _bridges.Count; i++) _bridges[i].PullFromEcs();
        }

        private void OnDestroy()
        {
            Instance = null;

            foreach (var system in _systems) system.Dispose();
            foreach (var ms in _managedSystems) ms.ManagedDispose();
            _world.Dispose();
        }
    }
}