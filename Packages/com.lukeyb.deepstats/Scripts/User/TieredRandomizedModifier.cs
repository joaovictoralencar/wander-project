using System.Collections;
using System.Collections.Generic;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.User;
using Unity.Mathematics;
using UnityEngine;

namespace LukeyB.DeepStats.User
{
    [System.Serializable]
    public class TieredRandomizedModifier
    {
        public float AdditionalScalingPerTier;
        public EditorDeepModifier TemplateModifier;

        public DeepModifier GetTieredModifier(int tier)
        {
            var newMod = TemplateModifier.Value;

            var baseValue = newMod.ModifyValue;
            var fixedIncrease = newMod.ModifyValue * AdditionalScalingPerTier * (tier - 1);
            var randomComponent = UnityEngine.Random.Range(0f, 1f) * newMod.ModifyValue * AdditionalScalingPerTier;

            newMod.ModifyValue = baseValue + fixedIncrease + randomComponent;

            return newMod;
        }
    }
}

