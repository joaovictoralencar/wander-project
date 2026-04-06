using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities.Editor;
using UnityEngine;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace HelloDev.Entities
{
    /// <summary>
    /// Root MonoBehaviour for ECS-driven GameObjects. Creates the entity, applies components
    /// and systems from the inspector lists, and auto-discovers child bridges.
    /// </summary>
    public class EcsEntityRoot : MonoBehaviour, IEntityContext
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

        [Tooltip("ECS components added to this entity at spawn. Only components with configurable initial values belong here — bridges declare what they provide via ProvidedComponents.")]
        [SerializeReference]
        public List<IComponentInitializer> Components = new();

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

            // Duplicate component initializers.
            var seenComponents = new HashSet<Type>();
            foreach (var init in Components)
            {
                if (init == null) continue;
                if (!seenComponents.Add(init.ComponentType))
                    _errors.Add($"Duplicate initializer: {init.ComponentType.Name} appears more than once.");
            }

            // Full component coverage: initializers + what bridges declare they provide.
            var coveredComponents = new HashSet<Type>(seenComponents);
            foreach (var bridge in GetComponentsInChildren<EcsComponentBridge>())
                foreach (var t in bridge.ProvidedComponents)
                    coveredComponents.Add(t);

            // All systems that will be active: explicitly listed + auto-registered by bridges.
            var allSystemTypes = new HashSet<Type>(Systems.Where(s => s != null).Select(s => s.GetType()));
            foreach (var bridge in GetComponentsInChildren<EcsComponentBridge>())
                foreach (var sysType in bridge.RequiredSystems)
                    allSystemTypes.Add(sysType);

            // Each active system's RequiredComponents must be covered.
            foreach (var sysType in allSystemTypes)
            {
                if (Activator.CreateInstance(sysType) is not EcsSystemBase sys) continue;
                foreach (var type in sys.RequiredComponents)
                    if (!coveredComponents.Contains(type))
                        _warnings.Add($"{sysType.Name} needs {type.Name} — add an initializer or ensure a bridge provides it.");
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
            EcsDebug.Log($"'{gameObject.name}' spawned as Entity({Entity.Id})");

            for (var i = 0; i < Components.Count; i++)
                Components[i]?.Apply(World, Entity);

            for (var i = 0; i < Systems.Count; i++)
                if (Systems[i] != null)
                    runner.AddSystem(Systems[i]);

            var bridges = GetComponentsInChildren<EcsComponentBridge>();
            for (var i = 0; i < bridges.Length; i++)
            {
                bridges[i].Initialize(this);
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