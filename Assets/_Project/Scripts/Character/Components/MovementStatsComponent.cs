using System;
using HelloDev.Entities;

namespace Wander.Character.Components
{
    [Serializable]
    public struct MovementStatsComponent
    {
        public float WalkSpeed;
        public float RunSpeed;
        public float JumpForce;
        public float Gravity;
        public float RotationSpeed;
    }

    [Serializable]
    public sealed class MovementStatsInitializer : ComponentInit<MovementStatsComponent>
    {
        public MovementStatsInitializer() => Component = new MovementStatsComponent
        {
            WalkSpeed     = 4f,
            RunSpeed      = 8f,
            JumpForce     = 7f,
            Gravity       = 18f,
            RotationSpeed = 12f,
        };
    }
}
