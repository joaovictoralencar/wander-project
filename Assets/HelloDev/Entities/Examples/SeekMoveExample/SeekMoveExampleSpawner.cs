using HelloDev.Entities.Components;
using Unity.Mathematics;
using UnityEngine;

namespace HelloDev.Entities.Examples
{
    /// <summary>
    /// Spawns N balls that chase a target Transform every frame using a Burst-compiled parallel job.
    ///
    /// Scene setup:
    ///   1. Add EcsSystemRunner to a GameObject (assign an EcsConfigAsset if you have one).
    ///   2. Add this component to another GameObject.
    ///   3. Create a target GameObject (e.g. a cube), assign it to Target in the Inspector.
    ///      Move it during Play Mode — all balls will steer toward it in real time.
    ///   4. Press Play.
    /// </summary>
    public class SeekMoveExampleSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private int _count = 12;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _spawnRadius = 5f;

        private SeekAndMoveSystem _seekSystem;

        private void Start()
        {
            var runner = EcsSystemRunner.Instance;
            if (runner == null)
            {
                Debug.LogError("[SeekMoveExample] EcsSystemRunner not found.", this);
                return;
            }

            // Register the system — AddSystem calls Initialize internally.
            _seekSystem = new SeekAndMoveSystem();
            runner.AddSystem(_seekSystem);

            var world = runner.World;

            for (int i = 0; i < _count; i++)
            {
                float angle = i * (360f / _count);
                var spawnPos = new float3(
                    math.cos(math.radians(angle)) * _spawnRadius,
                    0f,
                    math.sin(math.radians(angle)) * _spawnRadius);

                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = (Vector3)spawnPos;
                go.transform.localScale = Vector3.one * 0.4f;
                go.name = $"SeekEntity_{i}";

                // Manually initialize the bridge — bypasses auto-Start so we can add components now.
                var bridge = go.AddComponent<EcsEntityBridge>();
                var entity = world.CreateEntity();
                bridge.Initialize(world, entity, syncTransformToEcs: false, syncEcsToTransform: true, interpolatePosition: true);
                runner.RegisterBridge(bridge);

                world.AddComponent(entity, new PositionComponent { Value = spawnPos });
                world.AddComponent(entity, new PreviousPositionComponent { Value = spawnPos });
                world.AddComponent(entity, new VelocityComponent { Value = float3.zero, Speed = _speed });
            }
        }

        private void Update()
        {
            if (_seekSystem == null) return;
            // Set target position in Update — runs after FixedUpdate, so the value is always
            // stable and ready before the next fixed step picks it up.
            _seekSystem.TargetPosition = _target != null ? (float3)_target.position : float3.zero;
        }
    }
}
