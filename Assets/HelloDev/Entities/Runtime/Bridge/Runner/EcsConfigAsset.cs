using UnityEngine;

namespace HelloDev.Entities
{
    [CreateAssetMenu(fileName = "EcsConfig", menuName = "HelloDev/Entities/EcsConfig")]
    public class EcsConfigAsset : ScriptableObject
    {
        [Header("Set before play, this values cannot be changed at runtime")]
        public int MaxEntities = 128;
    }

    public static class EcsRuntime
    {
        // Cached once at startup — after this, the SO value is irrelevant.
        public static int MaxEntities { get; private set; } = 128; // safe default if Initialize is never called

        // Call this before creating your EcsWorld — enforces initialization order.
        public static void Initialize(EcsConfigAsset config)
        {
            MaxEntities = config.MaxEntities;

            // Validate the value makes sense before committing.
            Debug.Assert(MaxEntities is > 0 and <= 1024, "MaxEntities must be between 1 and 1024.");
        }
    }
}