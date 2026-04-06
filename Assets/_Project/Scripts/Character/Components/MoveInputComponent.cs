using System;
using Unity.Mathematics;
using HelloDev.Entities;

namespace Wander.Character.Components
{
    [Serializable]
    public struct MoveInputComponent
    {
        // World-space direction; magnitude encodes analog-stick strength (0–1).
        public float3 Direction;
        public bool Sprint;
        // Consumed once per FixedUpdate — set true on press, cleared after PushToEcs.
        public bool Jump;
    }

    [Serializable]
    public sealed class MoveInputInitializer : ComponentInit<MoveInputComponent> { }
}
