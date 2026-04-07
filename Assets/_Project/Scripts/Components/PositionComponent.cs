using System;
using Unity.Mathematics;

namespace Wander.Components
{
    [Serializable]
    public struct PositionComponent
    {
        [NonSerialized] public float3 Value;
    }
}
