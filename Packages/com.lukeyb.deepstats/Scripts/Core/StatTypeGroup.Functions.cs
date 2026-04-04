using System.Collections;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Core
{
    public partial struct StatTypeGroup
    {
        private const int SIZE = sizeof(int) * 8;

        public void SetStat(StatType statType, bool value)
        {
            var index = (int)statType;
            unsafe
            {
                fixed (int* ptr = &f0)
                {
                    var ptrOffset = index / SIZE;
                    int* intPtr = ptr + ptrOffset;
                    int bitIndex = index % SIZE;

                    if (value)
                    {
                        *intPtr |= (int)(1 << bitIndex); // Set the flag
                    }
                    else
                    {
                        *intPtr &= (int)~(1 << bitIndex); // Clear the flag
                    }
                }
            }
        }

        public bool IsStatSet(StatType statType)
        {
            var index = (int)statType;
            unsafe
            {
                fixed (int* ptr = &f0)
                {
                    int* intPtr = ptr + (index / SIZE);
                    int bitIndex = index % SIZE;
                    return (*intPtr & (1 << bitIndex)) != 0;
                }
            }
        }

        public void SetStatTypesFrom(in StatTypeGroup other)
        {
            unsafe
            {
                fixed (int* ptrA = &f0, ptrB = &other.f0)
                {
                    for (int i = 0; i < NumParts; i++)
                    {
                        *(ptrA + i) |= *(ptrB + i);
                    }
                }
            }
        }
    }
}