namespace HelloDev.Entities
{
    /// <summary>Participates in the runner's push/pull cycle each Fixed/Update.</summary>
    public interface IBridge
    {
        // Push Unity state into ECS before systems run each FixedUpdate.
        void PushToEcs();
        // Pull ECS results back to Unity after FixedExecute systems (e.g. CharacterController.Move).
        void FixedPullFromEcs();
        // Pull ECS state back to Unity after Execute systems each Update (visuals, animation).
        void PullFromEcs();
    }
}