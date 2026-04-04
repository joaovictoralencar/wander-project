using UnityEngine;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.User;
using System.Collections.Generic;

namespace LukeyB.DeepStats.User
{
    [CreateAssetMenu(fileName = "StatType", menuName = "DeepStats/Stat Type", order = 1)]
    public class StatTypeSO : ScriptableEnum<StatType>
    {
        [Header("Modifiers to the parent Stat Type will also apply to this Stat Type.\nThese can be chained together.")]
        public StatTypeSO ParentStat;
    }
}
