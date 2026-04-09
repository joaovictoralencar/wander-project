using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct CombatStatsComponent
    {
        // Config — tweakable in Inspector via bridge
        public float BaseDamage;
        public float AttackSpeed;      // future: animation speed multiplier
        public float CritChance;       // future: 0–1 probability
        public float CritMultiplier;   // future: e.g. 1.5x

        // Runtime — for buffs/debuffs (future)
        [NonSerialized] public float BonusDamage;
    }
}