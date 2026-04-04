using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Entities
{
    public class EcsSystemRunner : MonoBehaviour
    {
        public static EcsSystemRunner Instance { get; private set; }

        [SerializeField] private EcsConfigAsset _config;

        private EcsWorld _world;
        private readonly List<IEcsSystem> _systems = new();
        private readonly List<EcsEntityBridge> _bridges = new();
        private readonly EcsCommandBuffer _commandBuffer = new();

        public EcsWorld World => _world;

        private void Awake()
        {
            Instance = this;

            if (_config != null)
                EcsRuntime.Initialize(_config);
            else
                Debug.LogWarning("[ECS] No EcsConfigAsset assigned to EcsSystemRunner. Using default MaxEntities (128).");

            _world = new EcsWorld();

            // Register game systems here, or call AddSystem() from external MonoBehaviours in Start().
            // Example: _systems.Add(new MoveSystem());
        }

        // Can be called from Start() of any MonoBehaviour — world is guaranteed ready by then.
        public void AddSystem(IEcsSystem system)
        {
            _systems.Add(system);
            system.Initialize(_world);
        }

        public void RegisterBridge(EcsEntityBridge bridge) => _bridges.Add(bridge);
        public void UnregisterBridge(EcsEntityBridge bridge) => _bridges.Remove(bridge);

        private void FixedUpdate()
        {
            foreach (var bridge in _bridges) bridge.PushToEcs();

            foreach (var system in _systems) system.FixedExecute(_world, _commandBuffer, Time.fixedDeltaTime);

            _commandBuffer.Flush(_world);
            // PullFromEcs is intentionally NOT called here — visual sync happens in Update.
        }

        private void Update()
        {
            // Presentation pass — visual-only systems run here (interpolation, etc.).
            foreach (var system in _systems) system.Execute(_world, Time.deltaTime);

            // Sync ECS positions to Transforms. Bridges with PreviousPositionComponent will lerp.
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