using System;
using HelloDev.Entities;
using Unity.Mathematics;

namespace Wander.Components
{
    [Serializable]
    public struct PositionComponent
    {
        public float3 Value;
    }

    [Serializable]
    public sealed class PositionInitializer : ComponentInit<PositionComponent> { }
}
