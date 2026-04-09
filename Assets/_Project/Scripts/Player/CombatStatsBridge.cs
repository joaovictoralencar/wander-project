using HelloDev.Entities;
using UnityEngine;
using Wander.Character.Components;

namespace Wander.Player
{
    [Provides(typeof(CombatStatsComponent))]
    public class CombatStatsBridge : EcsComponentBridge
    {
        [SerializeField] private CombatStatsComponent _stats = new()
        {
            BaseDamage     = 10f,
            AttackSpeed    = 1f,
            CritChance     = 0f,
            CritMultiplier = 1.5f,
        };

        protected override void OnInitialize()
        {
            _stats.BonusDamage = 0f;
            Add(_stats);
        }
    }
}
