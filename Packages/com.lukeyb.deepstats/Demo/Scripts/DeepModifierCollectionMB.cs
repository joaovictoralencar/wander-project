using System.Collections.Generic;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    [DefaultExecutionOrder(-1)] // run before other scripts so that any connected deepstats are all hooked up in Start() by the time they are read
    public class DeepModifierCollectionMB : MonoBehaviour
    {
        [SerializeField] private List<EditorDeepModifier> _initialModifiers;

        [SerializeField] private List<DeepStatsMB> _initialStatsChildren;

        private bool _hasInitialised = false;

        public DeepModifierCollection ModifierCollection;

        private void Awake()
        {
            ModifierCollection = new DeepModifierCollection();

            for (var i = 0; i < _initialModifiers.Count; i++)
            {
                ModifierCollection.AddModifier(_initialModifiers[i]);
            }
        }

        private void Start()
        {
            if (_hasInitialised)
                return;

            for (var i = 0; i < _initialStatsChildren.Count; i++)
            {
                ModifierCollection.AddStatsChild(_initialStatsChildren[i].DeepStats);
            }

            _hasInitialised = true;
        }
    }
}