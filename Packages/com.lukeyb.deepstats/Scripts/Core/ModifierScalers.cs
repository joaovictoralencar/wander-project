using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System;
using LukeyB.DeepStats.User;

namespace LukeyB.DeepStats.Core
{
    public class ModifierScalers
    {
        public NativeArray<float> Array = new NativeArray<float>(DeepStatsConstants.NumModifierScalers, Allocator.Persistent);
        public Action ScalersChanged;

        public float this[ModifierScaler scaler]
        {
            set 
            { 
                if (Array[(int)scaler] != value)
                {
                    Array[(int)scaler] = value;
                    ScalersChanged?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            Array.Dispose();
        }
    }
}
