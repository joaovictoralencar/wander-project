using System.Collections.Generic;
using LukeyB.DeepStats.ScriptableObjects;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    [DefaultExecutionOrder(-1)] // run before other scripts so that any connected deepstats are all hooked up in Start() by the time they are read
    public class DeepStatsMB : MonoBehaviour
    {
        [SerializeField] private List<EditorDeepModifier> _initialModifiers;
        [SerializeField] private List<ModifierTagSO> _tags;
        [SerializeField] private List<DeepStatsMB> _initialAddedStatsSources;

        private bool _hasInitialised = false;

        public DeepStatsInstance DeepStats;

        private void Awake()
        {
            DeepStats = new DeepStatsInstance();
            for (var i = 0; i < _tags.Count; i++)
            {
                DeepStats.SetTag(_tags[i].EnumValue, true);
            }

            for (var i = 0; i < _initialModifiers.Count; i++)
            {
                DeepStats.AddModifier(_initialModifiers[i]);
            }
        }

        private void Start()
        {
            if (_hasInitialised)
                return;

            for (var i = 0; i < _initialAddedStatsSources.Count; i++)
            {
                DeepStats.AddAddedStatsSource(_initialAddedStatsSources[i].DeepStats);
            }

            _hasInitialised = true;
        }

        private void OnDestroy()
        {
            DeepStats.Dispose();
        }
    }
}