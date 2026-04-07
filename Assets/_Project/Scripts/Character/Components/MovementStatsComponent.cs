using System;

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
}
