using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using System;
using LukeyB.DeepStats.ScriptableObjects;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Core;

namespace LukeyB.DeepStats.GameObjects
{
    [DefaultExecutionOrder(-5)] // run before other deepstats so that we guarantee StatConstants are available
    public class DeepStatsManager : MonoBehaviour
    {
        [SerializeField] private DeepStatsConfigurationSO _statConfiguration;

        public static ModifierTagLookup DefaultTags;
        public static NativeArray<float> DefaultScalers;

        public static NativeArray<float2> TotalConversionPerStat;
        public static NativeList<TotalDependent> TotalDependent;

        public static NativeArray<StatType> ParentStats;

        // Static variable to track the instance of this component
        private static DeepStatsManager _instance;
        private bool _isFirstManager = false; // only manage the static members with the first instance, otherwise we'll overwrite references and try to dispose multiple times etc.

        private void Awake() 
        {
            if (_instance != null)
            {
                // If an instance already exists, destroy this object
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _isFirstManager = true;
            CreateRuntimeData();
        }

        // called when editor changes
        private void OnValidate()
        {
            if (_statConfiguration == null)
            {
                Debug.LogError("You must set a DeepStats Configuration in the DeepStatsManager");
            }
        }

        public void SetConfigurationEditorOnly(DeepStatsConfigurationSO config)
        {
            _statConfiguration = config;
        }

        private void CreateRuntimeData() 
        {
            OnDestroy();

            DefaultTags = new ModifierTagLookup();
            DefaultScalers = new NativeArray<float>(0, Allocator.Persistent);

            TotalConversionPerStat = new NativeArray<float2>(DeepStatsConstants.NumStatTypes, Allocator.Persistent);
            TotalDependent = new NativeList<TotalDependent>(Allocator.Persistent);

            BuildStatTypeGroups();
        }

        private void BuildStatTypeGroups()
        {
            ParentStats = new NativeArray<StatType>(DeepStatsConstants.NumStatTypes, Allocator.Persistent);

            for (var i = 0; i < _statConfiguration.StatTypes.Length; i++)
            {
                var st = _statConfiguration.StatTypes[i];

                ParentStats[i] = st.ParentStat != null ? st.ParentStat.EnumValue : (StatType)(-1);
            }
        }

        private void OnDestroy()
        {
            if (!_isFirstManager)
            {
                return;
            }

            if (DefaultScalers != null && DefaultScalers.IsCreated)
            {
                DefaultScalers.Dispose();
            }

            if (TotalConversionPerStat != null && TotalConversionPerStat.IsCreated)
            {
                TotalConversionPerStat.Dispose();
            }

            if (TotalDependent.IsCreated)
            {
                TotalDependent.Dispose();
            }

            if (ParentStats != null && ParentStats.IsCreated)
            {
                ParentStats.Dispose();
            }
        }
    }
}
