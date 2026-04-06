using System;
using HelloDev.Entities;
using Unity.Mathematics;

namespace Wander.Components
{
    // Stores the entity's position at the start of each FixedUpdate.
    // Used alongside PositionComponent to lerp the visual Transform.
    [Serializable]
    public struct PreviousPositionComponent
    {
        public float3 Value;
    }

    [Serializable]
    public sealed class PreviousPositionInitializer : ComponentInit<PreviousPositionComponent> { }
}
