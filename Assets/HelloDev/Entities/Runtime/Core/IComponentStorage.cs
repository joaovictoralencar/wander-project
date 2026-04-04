using System;

namespace HelloDev.Entities
{
    public interface IComponentStorage : IDisposable
    {
        void RemoveComponent(int entityId);
        bool HasComponent(int entityId);
    }
}