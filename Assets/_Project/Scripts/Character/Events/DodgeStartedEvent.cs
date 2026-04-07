using HelloDev.Entities;
using Unity.Mathematics;

namespace Wander.Character.Events
{
    /// <summary>Fired by <see cref="Wander.Character.Systems.DodgeSystem"/> the frame a dodge begins.</summary>
    public struct DodgeStartedEvent
    {
        public Entity Entity;
        public float3 Direction;
    }
}
