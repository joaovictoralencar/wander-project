namespace HelloDev.Entities
{
    /// <summary>
    /// Static cache for ECS runtime configuration. Set by <see cref="EcsSystemRunner"/> at startup.
    /// </summary>
    public static class EcsRuntime
    {
        public static int MaxEntities { get; internal set; } = 128;
    }
}