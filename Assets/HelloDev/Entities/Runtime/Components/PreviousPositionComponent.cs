using Unity.Mathematics;

namespace HelloDev.Entities.Components
{
    /// <summary>
    /// Written at the start of every FixedExecute before the entity moves.
    /// The bridge reads this alongside PositionComponent to lerp the visual Transform.
    /// Add this component to any entity that needs smooth interpolated rendering.
    /// </summary>
    public struct PreviousPositionComponent
    {
        public float3 Value;
    }
}
