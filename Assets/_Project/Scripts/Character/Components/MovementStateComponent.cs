using System;
using HelloDev.Entities;
using Unity.Mathematics;

namespace Wander.Character.Components
{
    /// <summary>
    /// Runtime movement state: velocity vector, scalar speed, and grounded status.
    /// Written by CharacterPhysicsSystem; IsGrounded is seeded each frame by MoveEntityBridge.
    /// </summary>
    [Serializable]
    public struct MovementStateComponent
    {
        public float3 Velocity;
        public float  Speed;
        public bool   IsGrounded;
    }

    [Serializable]
    public sealed class MovementStateInitializer : ComponentInit<MovementStateComponent>
    {
        public MovementStateInitializer() => Component = new MovementStateComponent { IsGrounded = true };
    }
}
