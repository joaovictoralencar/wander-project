using System.Collections;
using System.Collections.Generic;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.User
{
    [System.Serializable]
    public class TieredModifier
    {
        public float ScalingPerTier = 0.2f;
        public EditorDeepModifier TemplateModifier;

        public DeepModifier GetTieredModifier(int tier)
        {
            var newMod = TemplateModifier.Value;
            newMod.ModifyValue = newMod.ModifyValue * (1 + tier * ScalingPerTier);
            return newMod;
        }
    }
}
