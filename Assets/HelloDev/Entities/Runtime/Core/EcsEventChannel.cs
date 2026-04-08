using System;
using System.Collections.Generic;

namespace HelloDev.Entities
{
    /// <summary>
    /// Deferred-dispatch event channel. Producers call <see cref="Send"/> to buffer events;
    /// the runner calls <see cref="Flush"/> at controlled pipeline points to deliver them
    /// to all subscribers, then clears the buffer.
    /// </summary>
    public class EcsEventChannel<T> : IEcsEventChannel
    {
        private readonly List<T> _pending = new();
        private readonly List<Action<T>> _subscribers = new();

        public void Send(T e)
        {
            _pending.Add(e);
            EcsDebug.Log($"Event sent: {typeof(T).Name}");
        }

        public IDisposable Subscribe(Action<T> callback)
        {
            _subscribers.Add(callback);
            return new Subscription(this, callback);
        }

        public void Flush()
        {
            for (int i = 0; i < _pending.Count; i++)
                for (int j = 0; j < _subscribers.Count; j++)
                    _subscribers[j](_pending[i]);
            _pending.Clear();
        }

        public void Dispose()
        {
            _pending.Clear();
            _subscribers.Clear();
        }

        private sealed class Subscription : IDisposable
        {
            private readonly EcsEventChannel<T> _channel;
            private readonly Action<T> _callback;

            public Subscription(EcsEventChannel<T> channel, Action<T> callback)
            {
                _channel = channel;
                _callback = callback;
            }

            public void Dispose() => _channel._subscribers.Remove(_callback);
        }
    }
}
