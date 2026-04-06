using System;
using HelloDev.Entities;

namespace Wander.Character.Components
{
    [Serializable]
    public struct AnimationStateComponent
    {
        // Normalized 0–1: 0 = stationary, 1 = full run speed.
        public float SpeedBlend;
        public bool IsGrounded;
        // True for exactly one Execute frame when the entity first leaves the ground.
        public bool TriggerJump;
    }

    [Serializable]
    public sealed class AnimationStateInitializer : ComponentInit<AnimationStateComponent> { }
}
