using System;
using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Entities
{
    public static class ComponentRegistry
    {
        private static readonly Dictionary<Type, int> _typeIndex = new();
        private static readonly Dictionary<int, Type> _bitToType = new();
        private static int _nextIndex = 0;

        // Assigns a stable bit index to each component type the first time it's seen.
        public static int GetOrRegister(Type type)
        {
            if (!_typeIndex.TryGetValue(type, out var index))
            {
                Debug.Assert(_nextIndex < 30, $"[ECS] Component type limit reached. Max 30 types with an int bitmask. Consider migrating to long.");
                index = _nextIndex++;
                _typeIndex[type] = index;
                _bitToType[index] = type;
            }

            return index;
        }

        // Clears all registrations. Call before creating a new EcsWorld so stale bit
        // assignments don't accumulate when Enter Play Mode → Domain Reload is disabled.
        public static void Reset()
        {
            _typeIndex.Clear();
            _bitToType.Clear();
            _nextIndex = 0;
        }

        // Builds a bitmask with a 1 in the position for each provided type.
        public static int BuildMask(params Type[] types)
        {
            int mask = 0;
            foreach (var type in types)
                mask |= 1 << GetOrRegister(type);
            return mask;
        }

        /// <summary>
        /// Returns the component types encoded in a bitmask.
        /// Only types that have been registered via <see cref="GetOrRegister"/> or
        /// <see cref="BuildMask"/> are returned — unknown bits are silently skipped.
        /// </summary>
        public static IEnumerable<Type> GetTypesFromMask(int mask)
        {
            for (int i = 0; i < 30; i++)
                if ((mask & (1 << i)) != 0 && _bitToType.TryGetValue(i, out var type))
                    yield return type;
        }
    }
}