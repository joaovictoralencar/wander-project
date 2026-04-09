using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a combo step begins.</summary>
    public struct AttackStepStartedEvent
    {
        public Entity Entity;
        public int    ComboIndex;
        public int    StepIndex;
    }
}
