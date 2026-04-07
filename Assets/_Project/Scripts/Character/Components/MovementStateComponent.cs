using System;
using Unity.Mathematics;

namespace Wander.Character.Components
{
    /// <summary>
    /// Runtime movement state: velocity vector, scalar speed, and grounded status.
    /// Written by CharacterPhysicsSystem; IsGrounded is seeded each frame by MoveEntityBridge.
    /// All fields are runtime — nothing here is designer-configurable.
    /// </summary>
    [Serializable]
    public struct MovementStateComponent
    {
        [NonSerialized] public float3 Velocity;
        [NonSerialized] public float  Speed;
        [NonSerialized] public bool   IsGrounded;
        [NonSerialized] public bool   CanMove;
    }
}