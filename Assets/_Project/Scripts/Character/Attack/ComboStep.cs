using System;
using UnityEngine;

namespace Wander.Character.Attack
{
    [Serializable]
    public class ComboStep
    {
        [Header("Animation")] public AnimationClip Clip;
        [Tooltip("Multiplier for Clip length (1 = normal speed)")]
        public float PlayRate = 1f;

        [Header("Damage")] [Tooltip("Multiplied by CombatStatsComponent.BaseDamage at hit time")]
        public float DamageMultiplier = 1f;
        
    }
}