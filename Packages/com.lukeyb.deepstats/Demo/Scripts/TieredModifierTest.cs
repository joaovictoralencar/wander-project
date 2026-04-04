using System.Collections;
using System.Collections.Generic;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    public class TieredModifierTest : MonoBehaviour
    {
        public int NumTiers = 5;
        public TieredModifier Mod;

        private void Awake()
        {
            for (var i = 1; i <= NumTiers; i++)
            {
                var mod = Mod.GetTieredModifier(i);
                Debug.Log($"Modifier Tier {i}: {mod.ModifyValue}");
            }
        }
    }
}
