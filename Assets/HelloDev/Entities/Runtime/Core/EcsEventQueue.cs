using System.Collections.Generic;

namespace HelloDev.Entities
{
    /// <summary>
    /// A single-frame event channel. Events are written by systems or bridges during a FixedUpdate,
    /// readable by any system or bridge in the same frame (including Update-time bridges),
    /// and cleared at the start of the next FixedUpdate by <see cref="EcsSystemRunner"/>.
    /// </summary>
    public class EcsEventQueue<T> : IEcsEventQueue
    {
        private readonly List<T> _events = new();

        public void Enqueue(T e)
        {
            _events.Add(e);
            EcsDebug.Log($"Event queued: {typeof(T).Name}");
        }
        public IReadOnlyList<T> Read() => _events;
        public bool HasAny => _events.Count > 0;
        public void Clear() => _events.Clear();
    }
}
