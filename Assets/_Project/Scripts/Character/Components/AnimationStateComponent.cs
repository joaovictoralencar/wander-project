using System;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AnimationStateComponent
    {
        [NonSerialized] public float SpeedBlend;
        [NonSerialized] public bool  IsGrounded;
    }
}
