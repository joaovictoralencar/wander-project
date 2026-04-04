using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    public class TieredRandomizedModifierTest : MonoBehaviour
    {
        public int NumUpgrades = 5;
        public TieredRandomizedModifier Mod;

        private void Awake()
        {
            for (var i = 1; i <= NumUpgrades; i++)
            {
                var mod = Mod.GetTieredModifier(i);
                Debug.Log($"Randomised Modifier Level {i}: {mod.ModifyValue.x}");
            }
        }
    }
}
