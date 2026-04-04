using System.Collections.Generic;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo 
{
    [System.Serializable]
    public class UnitScaler
    {
        public ModifierScalerSO Scaler;
        public float ScalerValue;
    }

    public class GameUnit : MonoBehaviour
    {
        public List<EditorDeepModifier> Modifiers;
        public List<ModifierTagSO> UnitTags;
        public List<UnitScaler> UnitScalers;

        private DeepStatsInstance _stats;

        public void Awake()
        {
            _stats = new DeepStatsInstance();

            foreach (var tag in UnitTags)
            {
                _stats.SetTag(tag, true);
            }

            foreach (var scaler in UnitScalers)
            {
                _stats.SetScaler(scaler.Scaler, scaler.ScalerValue);
            }

            foreach (var m in Modifiers)
            {
                _stats.AddModifier(m.Value);
            }

            _stats.UpdateFinalValues(null);

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                var statType = (StatType)i;
                var value = _stats.GetFinalRange(statType);
                Debug.Log($"{statType}: {value}");
            }
        }

        private void OnDestroy()
        {
            _stats.Dispose();
        }
    }
}
