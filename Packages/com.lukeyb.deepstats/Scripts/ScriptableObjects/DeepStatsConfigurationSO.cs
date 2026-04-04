
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.ScriptableObjects
{
    [CreateAssetMenu(fileName = "StatConfiguration", menuName = "DeepStats/Stat Configuration", order = 0)]
    public class DeepStatsConfigurationSO : ScriptableObject
    {
        public StatTypeSO[] StatTypes;
        public ModifierScalerSO[] ModifierScalers;
        public ModifierTagSO[] ModifierTags;
    }
}
