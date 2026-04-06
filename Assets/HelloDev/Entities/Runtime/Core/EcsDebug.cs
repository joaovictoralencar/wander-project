using UnityEngine;

namespace HelloDev.Entities
{
    /// <summary>
    /// Lightweight logging helper for the ECS framework.
    /// Toggle via <see cref="EcsSystemRunner"/>'s Debug Logs field in the inspector.
    /// </summary>
    public static class EcsDebug
    {
        /// <summary>Set by EcsSystemRunner on Awake.</summary>
        public static bool Enabled { get; internal set; }

        /// <summary>Extra per-frame logs (system entity counts). Separate toggle to avoid console spam.</summary>
        public static bool Verbose { get; internal set; }

        internal static void Log(string message)
        {
            if (Enabled) Debug.Log($"[ECS] {message}");
        }

        internal static void Warn(string message)
        {
            if (Enabled) Debug.LogWarning($"[ECS] {message}");
        }
    }
}
