using System;
using Unity.Mathematics;
using Wander.Character.Attack;

namespace Wander.Character.Components
{
    [Serializable]
    public struct MoveInputComponent
    {
        // All runtime — written by PlayerInputBridge each frame
        [NonSerialized] public float3 Direction;
        [NonSerialized] public bool Sprint;
        [NonSerialized] public bool Jump;
        [NonSerialized] public bool Dodge;
        [NonSerialized] public AttackInputType AttackInput;
    }
}