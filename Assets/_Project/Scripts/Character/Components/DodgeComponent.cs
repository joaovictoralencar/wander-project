using System;
using Unity.Mathematics;

namespace Wander
{
    [Serializable]
    public struct DodgeComponent
    {
        // Config — tweakable in Inspector via bridge
        public float DodgeDuration;
        public float DodgeSpeed;
        public float Cooldown;
        public float DodgeMaxBufferTime;


        // Runtime state — not serialized
        [NonSerialized] public bool   IsDodging;
        [NonSerialized] public float  ElapsedTime;
        [NonSerialized] public float3 Direction;
        [NonSerialized] public float  CooldownRemaining;
    }
}