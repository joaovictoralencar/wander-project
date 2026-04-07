using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by CharacterPhysicsSystem the moment jump force is applied.</summary>
    public struct JumpStartedEvent
    {
        public Entity Entity;
    }
}
