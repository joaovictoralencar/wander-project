using System;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.Enums;
using LukeyB.DeepStats.ScriptableObjects;
using Unity.Mathematics;
using UnityEngine;

namespace LukeyB.DeepStats.User
{
    [Serializable]
    public class EditorDeepModifier
    {
        [SerializeField] private ModifierType ModifierType;
        [SerializeField] private float2 ModifyValue;
        [SerializeField] private MinMaxValues MinMaxValues;
        [SerializeField] private StatTypeSO TargetStat;

        [SerializeField] private StatTypeSO DependentStat;
        [SerializeField] private CopyableModifyType TargetModifyTypes;

        [SerializeField] private DynamicSource ModifierScalingSource;
        [SerializeField] private ModifierScalerSO ModifierScalerType;
        [SerializeField] private float2 ScalingClamp;

        [SerializeField] private ModifierTagSO[] SelfTags;
        [SerializeField] private ModifierTagSO[] TargetTags;

        [SerializeField] private string ValidationError;

        [NonSerialized]
        private DeepModifier _modifier;
        [NonSerialized] // no idea why this is necessary, but Unity was serializing this value causing it to not rebuild when used in persistent objects like scriptable objects
        private bool _created;

        public DeepModifier Value
        {
            get
            {
                if (ValidationError != "")
                {
                    throw new BadModifierConfigurationException($"Could not create modifier. Reason: {ValidationError}");
                }

                if (!_created)
                {
                    CreateModifier(true);
                }

                return _modifier;
            }
        }

        public EditorDeepModifier(ModifierType modifyType, float2 modifyValue, MinMaxValues minMaxValues, StatTypeSO targetStat, StatTypeSO dependentStat, CopyableModifyType targetModifyTypes, 
            DynamicSource modifierScalingSources, ModifierScalerSO modifierScalerType, ModifierTagSO[] selfTags, ModifierTagSO[] targetTags)
        {
            ModifierType = modifyType;
            ModifyValue = modifyValue;
            MinMaxValues = minMaxValues;
            TargetStat = targetStat;
            DependentStat = dependentStat;
            TargetModifyTypes = targetModifyTypes;

            ModifierScalingSource = modifierScalingSources;
            ModifierScalerType = modifierScalerType;

            SelfTags = selfTags;
            TargetTags = targetTags;
        }

        /// <summary>
        /// Force a reconstruction of the underlying DeepModifier struct value.  
        /// This wont update any DeepStats this Modifier has already been added to.
        /// You'll have to remove it and add it again if you want to update DeepStats instances
        /// </summary>
        public void ForceReconstruct()
        {
            if (!_created)
            {
                CreateModifier(true);   // create a whole new struct
            }
            {
                CreateModifier(false);  // create a new struct but dont change the identifier
            }
        }

        private void CreateModifier(bool updateIdentifier)
        {
            _created = true;

            var selfTagLookup = new ModifierTagLookup();
            for (var i = 0; i < SelfTags.Length; i++)
            {
                selfTagLookup.SetTag(SelfTags[i].EnumValue, true);
            }

            var targetTagLookup = new ModifierTagLookup();
            for (var i = 0; i < TargetTags.Length; i++)
            {
                targetTagLookup.SetTag(TargetTags[i].EnumValue, true);
            }

            _modifier = new DeepModifier()
            {
                ModifierType = ModifierType,
                ModifyValue = ModifyValue,
                MinMaxValues = MinMaxValues,
                TargetStat = TargetStat == null ? 0 : TargetStat.EnumValue,

                DependentStat = DependentStat == null ? 0 : DependentStat.EnumValue,
                TargetModifyTypes = TargetModifyTypes,

                ModifierScalingSource = ModifierScalingSource,
                ModifierScalerType = ModifierScalerType == null ? 0 : ModifierScalerType.EnumValue,
                ScalingClamp = ScalingClamp,

                SelfTags = selfTagLookup,
                TargetTags = targetTagLookup,

                ModifierIdentifier = updateIdentifier ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : _modifier.ModifierIdentifier
            };
        }
    }
}