using System.Collections;
using LukeyB.DeepStats.User;
using Unity.Mathematics;
using UnityEngine;

namespace LukeyB.DeepStats.Core
{
    public struct TotalDependent
    {
        public StatType DependentStat;
        public StatType TargetStat;
        public float2 TotalConversion;
        public float2 TotalAddedAs;

        public TotalDependent(StatType dependentStat, StatType targetStat, float2 totalConversion, float2 totalAddedAs)
        {
            DependentStat = dependentStat;
            TargetStat = targetStat;
            TotalConversion = totalConversion;
            TotalAddedAs = totalAddedAs;
        }
    }
}