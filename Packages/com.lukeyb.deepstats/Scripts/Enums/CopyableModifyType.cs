using System.Collections;
using UnityEngine;

namespace LukeyB.DeepStats.Enums
{
    [System.Flags]
    public enum CopyableModifyType
    {
        None = 0,
        BaseAdd = 1,
        SumMultiply = 2,
        ProductMultiply = 4,
        All = ~0
    }
}