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
                Debug.Assert(_nextIndex < 62, $"[ECS] Component type limit reached. Max 62 types with a long bitmask.");
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
        public static long BuildMask(params Type[] types)
        {
            long mask = 0;
            foreach (var type in types)
                mask |= 1L << GetOrRegister(type);
            return mask;
        }

        /// <summary>
        /// Returns the component types encoded in a bitmask.
        /// Only types that have been registered via <see cref="GetOrRegister"/> or
        /// <see cref="BuildMask"/> are returned — unknown bits are silently skipped.
        /// </summary>
        public static IEnumerable<Type> GetTypesFromMask(long mask)
        {
            for (int i = 0; i < 62; i++)
                if ((mask & (1L << i)) != 0 && _bitToType.TryGetValue(i, out var type))
                    yield return type;
        }
    }
}