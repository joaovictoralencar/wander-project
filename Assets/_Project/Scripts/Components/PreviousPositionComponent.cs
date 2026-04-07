using System;
using Unity.Mathematics;

namespace Wander.Components
{
    [Serializable]
    public struct PreviousPositionComponent
    {
        [NonSerialized] public float3 Value;
    }
}
