using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AnimationStateComponent
    {
        // All runtime — written by AnimationStateSystem, read by AnimationBridge
        [NonSerialized] public float SpeedBlend;
        [NonSerialized] public bool  IsGrounded;
        [NonSerialized] public bool  TriggerJump;
        [NonSerialized] public bool  TriggerDodge;
        [NonSerialized] public bool  IsDodging;
    }
}
