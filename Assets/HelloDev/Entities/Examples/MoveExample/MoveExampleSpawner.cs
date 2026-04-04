using HelloDev.Entities.Components;
using UnityEngine;
using Unity.Mathematics;

namespace HelloDev.Entities.Examples
{
    /// <summary>
    /// Drop this on any GameObject alongside an EcsSystemRunner in the scene.
    /// It spawns N objects that move outward from the origin, driven entirely by MoveSystem.
    ///
    /// Scene setup:
    ///   1. Create an empty GameObject, add EcsSystemRunner (assign an EcsConfigAsset if you have one).
    ///   2. Create another GameObject, add this component.
    ///   3. Optionally assign a Prefab — if left empty a default sphere is used.
    ///   4. Press Play.
    /// </summary>
    public class MoveExampleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count = 8;
        [SerializeField] private float _speed = 3f;
        [SerializeField] private float _spawnRadius = 2f;

        private void Start()
        {
            var runner = EcsSystemRunner.Instance;
            if (runner == null)
            {
                Debug.LogError("[MoveExample] EcsSystemRunner not found. Add it to the scene before this spawner.", this);
                return;
            }

            runner.AddSystem(new MoveSystem());

            var world = runner.World;

            for (int i = 0; i < _count; i++)
            {
                // Spread spawn positions evenly around a circle on the XZ plane.
                float angle = i * (360f / _count);
                var spawnPos = new float3(
                    math.cos(math.radians(angle)) * _spawnRadius,
                    0f,
                    math.sin(math.radians(angle)) * _spawnRadius);

                // Each entity moves directly away from the origin.
                var direction = math.normalizesafe(spawnPos);

                // Create (or spawn) the visual GameObject.
                var go = _prefab != null
                    ? Instantiate(_prefab, (Vector3)spawnPos, Quaternion.identity)
                    : CreateDefaultSphere((Vector3)spawnPos);
                go.name = $"MoveEntity_{i}";

                // Manually initialize the bridge so WE control when/how the entity is created.
                // (Bypasses the auto-init in EcsEntityBridge.Start so we can add components right now.)
                var bridge = go.AddComponent<EcsEntityBridge>();
                var entity = world.CreateEntity();
                bridge.Initialize(world, entity, syncTransformToEcs: false, syncEcsToTransform: true, interpolatePosition: true);
                runner.RegisterBridge(bridge);

                // Seed the ECS components — this is what MoveSystem will read each frame.
                world.AddComponent(entity, new PositionComponent { Value = spawnPos });
                world.AddComponent(entity, new PreviousPositionComponent { Value = spawnPos });
                world.AddComponent(entity, new VelocityComponent { Value = direction, Speed = _speed });
            }
        }

        private static GameObject CreateDefaultSphere(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.4f;
            return go;
        }
    }
}
