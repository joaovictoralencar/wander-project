using System;
using Unity.Mathematics;

namespace Wander.Character.Components
{
    [Serializable]
    public struct MoveInputComponent
    {
        // All runtime — written by PlayerInputBridge each frame
        [NonSerialized] public float3 Direction;
        [NonSerialized] public bool   Sprint;
        [NonSerialized] public bool   Jump;
        [NonSerialized] public bool   Attack;
        [NonSerialized] public bool   Dodge;
    }
}
