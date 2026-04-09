using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a combo/attack fully ends.</summary>
    public struct AttackEndedEvent
    {
        public Entity Entity;
    }
}
