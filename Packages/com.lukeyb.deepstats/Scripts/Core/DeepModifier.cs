using System;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Enums;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace LukeyB.DeepStats.Core
{
    public struct DeepModifier
    {
        public ModifierType ModifierType;
        public MinMaxValues MinMaxValues;
        public float2 ModifyValue;
        public StatType DependentStat;
        public StatType TargetStat;
        public CopyableModifyType TargetModifyTypes;
        public DynamicSource ModifierScalingSource;
        public ModifierScaler ModifierScalerType;
        public float2 ScalingClamp;
        public ModifierTagLookup SelfTags;
        public ModifierTagLookup TargetTags;
        public ModifierSource ModifierSource;
        public int ModifierIdentifier;

        public DeepModifier(ModifierType modifyType, float modifyValue, MinMaxValues minMaxValues, StatType targetStat)
        {
            ModifierType = modifyType;
            MinMaxValues = minMaxValues;
            ModifyValue = modifyValue;
            DependentStat = 0;
            TargetStat = targetStat;
            TargetModifyTypes = CopyableModifyType.None;
            ModifierScalingSource = 0;
            ModifierScalerType = 0;
            ScalingClamp = new float2(float.MinValue, float.MaxValue);
            SelfTags = new ModifierTagLookup();
            TargetTags = new ModifierTagLookup();
            ModifierSource = ModifierSource.Self;
            ModifierIdentifier = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        public DeepModifier(ModifierType modifyType, float modifyValue, MinMaxValues minMaxValues, StatType dependentStat, StatType targetStat)
        {
            ModifierType = modifyType;
            MinMaxValues = minMaxValues;
            ModifyValue = modifyValue;
            DependentStat = dependentStat;
            TargetStat = targetStat;
            TargetModifyTypes = CopyableModifyType.None;
            ModifierScalingSource = 0;
            ModifierScalerType = 0;
            ScalingClamp = new float2(float.MinValue, float.MaxValue);
            SelfTags = new ModifierTagLookup();
            TargetTags = new ModifierTagLookup();
            ModifierSource = ModifierSource.Self;
            ModifierIdentifier = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
    }
}
