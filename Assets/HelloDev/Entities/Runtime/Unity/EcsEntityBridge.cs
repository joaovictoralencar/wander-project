using HelloDev.Entities.Components;
using Unity.Mathematics;
using UnityEngine;

namespace HelloDev.Entities
{
    public class EcsEntityBridge : MonoBehaviour
    {
        [SerializeField] private bool _syncTransformToEcs;
        [SerializeField] private bool _syncEcsToTransform;
        /// <summary>
        /// When true, the Transform is lerped between PreviousPositionComponent and
        /// PositionComponent each Update frame. The entity must have PreviousPositionComponent
        /// added for this to have any effect.
        /// </summary>
        [SerializeField] private bool _interpolatePosition;

        public Entity Entity { get; private set; }
        private EcsWorld _world;

        // Called automatically in Start for scene-placed bridges.
        // Call manually (before Start) when spawning bridges from code.
        public void Initialize(EcsWorld world, Entity entity,
            bool syncTransformToEcs = false,
            bool syncEcsToTransform = false,
            bool interpolatePosition = false)
        {
            _world = world;
            Entity = entity;
            _syncTransformToEcs = syncTransformToEcs;
            _syncEcsToTransform = syncEcsToTransform;
            _interpolatePosition = interpolatePosition;
        }

        // Start runs after all Awake calls, so EcsSystemRunner.Instance is guaranteed to exist.
        private void Start()
        {
            if (_world != null) return; // already manually initialized — caller is responsible for RegisterBridge

            var runner = EcsSystemRunner.Instance;
            if (runner == null)
            {
                Debug.LogError("[EcsEntityBridge] EcsSystemRunner not found. Add it to the scene.", this);
                return;
            }

            var entity = runner.World.CreateEntity();
            Initialize(runner.World, entity, _syncTransformToEcs, _syncEcsToTransform, _interpolatePosition);
            runner.RegisterBridge(this);
        }

        private void OnDestroy()
        {
            if (_world == null) return;

            // If the runner is already gone (Instance == null), the world has been disposed —
            // touching any NativeCollection would throw ObjectDisposedException.
            if (EcsSystemRunner.Instance != null)
            {
                EcsSystemRunner.Instance.UnregisterBridge(this);
                if (_world.IsAlive(Entity))
                    _world.DestroyEntity(Entity);
            }
        }

        // Called BEFORE systems run — pushes Unity Transform state into ECS.
        public void PushToEcs()
        {
            if (!_syncTransformToEcs) return;
            _world.SetComponent(Entity, new PositionComponent { Value = transform.position });
        }

        // Called from EcsSystemRunner.Update — after FixedExecute has run at least once.
        // Lerps between PreviousPositionComponent and PositionComponent if interpolation is on.
        public void PullFromEcs()
        {
            if (!_syncEcsToTransform) return;

            var current = _world.GetComponent<PositionComponent>(Entity).Value;

            if (_interpolatePosition && _world.HasComponent<PreviousPositionComponent>(Entity))
            {
                var previous = _world.GetComponent<PreviousPositionComponent>(Entity).Value;
                float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                transform.position = Vector3.Lerp((Vector3)previous, (Vector3)current, alpha);
            }
            else
            {
                transform.position = (Vector3)current;
            }
        }
    }
}