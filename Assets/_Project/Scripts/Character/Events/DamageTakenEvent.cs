using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by DamageSystem after damage is applied to an entity.</summary>
    public struct DamageTakenEvent
    {
        public Entity Entity;
        public float  DamageAmount;
        public float  RemainingHealth;
    }
}
