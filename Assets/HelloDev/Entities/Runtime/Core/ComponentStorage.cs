using Unity.Collections;

namespace HelloDev.Entities
{
    public class ComponentStorage<T> : IComponentStorage where T : unmanaged
    {
        //Native array lives in unmanaged memory, so Burst and Jobs can read it safely
        public NativeArray<T> Data;

        //A bit per entity: 1 = this entity has this component, 0 = it doesn't
        private NativeBitArray _presence;

        public ComponentStorage(int maxEntities)
        {
            Data = new NativeArray<T>(maxEntities, Allocator.Persistent);
            _presence = new NativeBitArray(maxEntities, Allocator.Persistent);
        }

        public void Set(int entityId, T component)
        {
            Data[entityId] = component;
            _presence.Set(entityId, true);
        }

        public T Get(int entityId)
        {
            return Data[entityId];
        }

        public bool HasComponent(int entityId) => _presence.IsSet(entityId);

        public void RemoveComponent(int entityId) => _presence.Set(entityId, false);

        public void Dispose()
        {
            Data.Dispose();
            _presence.Dispose();
        }
    }
}