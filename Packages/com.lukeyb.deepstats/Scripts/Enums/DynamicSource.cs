using System.Collections;
using System.Diagnostics.Tracing;
using UnityEngine;

namespace LukeyB.DeepStats.Enums
{
    [System.Flags]
    public enum DynamicSource
    {
        None = 0,
        Self = 1,
        Target= 2,
        Both = ~0
    }
}