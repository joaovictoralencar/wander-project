using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct HealthComponent
    {
        // Config
        public float BaseHealth;

        // Runtime
        [NonSerialized] public float MaxHealth;
        [NonSerialized] public float CurrentHealth;
        [NonSerialized] public bool  IsDead;
    }
}
