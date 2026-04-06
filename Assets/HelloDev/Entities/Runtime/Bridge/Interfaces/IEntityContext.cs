namespace HelloDev.Entities
{
    /// <summary>
    /// Provides a bridge with the ECS <see cref="Entity"/> and <see cref="EcsWorld"/> it belongs to.
    /// Implemented by <see cref="EcsEntityRoot"/>.
    /// </summary>
    public interface IEntityContext
    {
        Entity Entity { get; }
        EcsWorld World { get; }
    }
}
