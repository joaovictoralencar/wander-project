using System.Collections;
using UnityEngine;

namespace LukeyB.DeepStats.Enums
{
    public enum ModifierType
    {
        Add,
        SumMultiply,
        ProductMultiply,
        AddedAs,
        ConvertedTo,
        ModifiersAlsoApplyToStat,
        ConvertSelfTags,
        ConvertTargetTags,

        FinalAdd = 1000,
        FinalSumMultiply,
        FinalProductMultiply
    }
}
