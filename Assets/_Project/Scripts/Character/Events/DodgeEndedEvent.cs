using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by <see cref="Wander.Character.Systems.DodgeSystem"/> the frame a dodge ends.</summary>
    public struct DodgeEndedEvent
    {
        public Entity Entity;
    }
}
