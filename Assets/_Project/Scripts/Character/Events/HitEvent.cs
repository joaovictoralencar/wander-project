using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackBridge when hitbox collides with a target entity.</summary>
    public struct HitEvent
    {
        public Entity Attacker;
        public Entity Target;
        public float  Damage;
    }
}
