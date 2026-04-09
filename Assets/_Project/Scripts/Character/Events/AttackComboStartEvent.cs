using HelloDev.Entities;

namespace Wander.Character.Events
{
    /// <summary>Fired by AttackSystem when a new combo begins. Bridge resolves which combo to play.</summary>
    public struct AttackComboStartEvent
    {
        public Entity Entity;
    }
}
