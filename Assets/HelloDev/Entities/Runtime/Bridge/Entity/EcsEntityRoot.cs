using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
#endif

namespace HelloDev.Entities
{
    /// <summary>
    /// Root MonoBehaviour for ECS-driven GameObjects. Creates the entity,
    /// auto-discovers and initializes child bridges, and registers orphan systems.
    /// </summary>
    public class EcsEntityRoot : MonoBehaviour
    {
#if UNITY_EDITOR && ODIN_INSPECTOR
        [OnInspectorGUI, PropertyOrder(-1)]
        private void DrawValidation()
        {
            foreach (var msg in _errors)
                SirenixEditorGUI.ErrorMessageBox(msg);
            foreach (var msg in _warnings)
                SirenixEditorGUI.WarningMessageBox(msg);
        }
#endif

        [Tooltip("Optional. Bridges auto-register their own required systems. Add systems here only when they are not tied to any bridge on this entity (e.g. global AI or utility systems).")]
        [SerializeReference]
        public List<EcsSystemBase> Systems = new();

        public Entity Entity { get; private set; }
        public EcsWorld World { get; private set; }

#if UNITY_EDITOR
        private readonly List<string> _errors   = new();
        private readonly List<string> _warnings = new();

        private void OnValidate()
        {
            _errors.Clear();
            _warnings.Clear();

            // Only consider bridges owned by THIS root, not nested EcsEntityRoot children.
            var allBridges = GetComponentsInChildren<EcsComponentBridge>();
            var bridges = new List<EcsComponentBridge>();
            foreach (var b in allBridges)
                if (b.GetComponentInParent<EcsEntityRoot>() == this)
                    bridges.Add(b);

            // Collect all provided components from bridge [Provides] attributes.
            var coveredComponents = new HashSet<Type>();
            foreach (var bridge in bridges)
            {
                foreach (var t in EcsComponentBridge.GetProvidedComponentTypes(bridge.GetType()))
                    if (!coveredComponents.Add(t))
                        _errors.Add($"Duplicate component: {t.Name} is provided by multiple bridges.");
            }

            // Collect all systems: explicitly listed + auto-registered by bridge [RequiresSystem] attributes.
            var allSystemTypes = new HashSet<Type>(Systems.Where(s => s != null).Select(s => s.GetType()));
            foreach (var bridge in bridges)
                foreach (var sysType in EcsComponentBridge.GetRequiredSystemTypes(bridge.GetType()))
                    allSystemTypes.Add(sysType);

            // Each system's RequiredComponents must be covered by a bridge.
            foreach (var sysType in allSystemTypes)
            {
                try
                {
                    if (Activator.CreateInstance(sysType) is not EcsSystemBase sys) continue;
                    foreach (var type in sys.RequiredComponents)
                        if (!coveredComponents.Contains(type))
                            _warnings.Add($"{sysType.Name} needs {type.Name} — ensure a bridge provides it.");
                }
                catch (Exception ex)
                {
                    _warnings.Add($"Cannot validate {sysType.Name}: {ex.Message}");
                }
            }
        }
#endif

        private void Awake()
        {
            var runner = EcsSystemRunner.Instance;
            if (runner == null)
            {
                Debug.LogError("[EcsEntityRoot] EcsSystemRunner not found. Add it to the scene.", this);
                return;
            }

            World  = runner.World;
            Entity = World.CreateEntity();

            if (Entity.IsNull)
            {
                Debug.LogError($"[EcsEntityRoot] Failed to create entity for '{gameObject.name}'. Entity limit reached.", this);
                return;
            }

            EcsDebug.Log($"'{gameObject.name}' spawned as Entity({Entity.Id})");

            // Orphan systems (not tied to any bridge).
            for (var i = 0; i < Systems.Count; i++)
                if (Systems[i] != null)
                    runner.AddSystem(Systems[i]);

            // Discover and initialize bridges on THIS root only — skip bridges that
            // belong to a nested EcsEntityRoot (they'll be initialized by their own root).
            var bridges = GetComponentsInChildren<EcsComponentBridge>();
            for (var i = 0; i < bridges.Length; i++)
            {
                // Walk up to find the closest EcsEntityRoot — if it's not us, skip this bridge.
                var closestRoot = bridges[i].GetComponentInParent<EcsEntityRoot>();
                if (closestRoot != this) continue;

                bridges[i].Initialize(World, Entity);
                runner.RegisterBridge(bridges[i]);
            }
        }

        private void OnDestroy()
        {
            if (World == null) return;
            if (EcsSystemRunner.Instance != null && World.IsAlive(Entity))
                World.DestroyEntity(Entity);
        }
    }
}