using System;
using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.Entities
{
    public class ComponentRegistry
    {
        private readonly Dictionary<Type, int> _typeIndex = new();
        private int _nextIndex = 0;

        // Assigns a stable bit index to each component type the first time it's seen.
        public int GetOrRegister(Type type)
        {
            if (!_typeIndex.TryGetValue(type, out var index))
            {
                Debug.Assert(_nextIndex < 30, $"[ECS] Component type limit reached. Max 30 types with an int bitmask. Consider migrating to long.");
                index = _nextIndex++;
                _typeIndex[type] = index;
            }
            return index;
        }
        
        // Builds a bitmask with a 1 in the position for each provided type.
        public int BuildMask(params Type[] types)
        {
            int mask = 0;
            foreach (var type in types)
                mask |= 1 << GetOrRegister(type);
            return mask;
        }
    }
}