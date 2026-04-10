using System;
using Unity.Mathematics;

namespace Wander.Components
{
    [Serializable]
    public struct TransformSnapshotComponent
    {
        [NonSerialized] public float3 Position;
        [NonSerialized] public quaternion Rotation;
        [NonSerialized] public float3 Forward;
        [NonSerialized] public float3 Right;
        [NonSerialized] public float3 Scale;
    }
}

