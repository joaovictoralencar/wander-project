
namespace LukeyB.DeepStats.User
{
    public partial struct ModifierTagLookup
    {
        private const int SIZE = sizeof(int) * 8;

        public void SetTag(ModifierTag tag, bool value)
        {
            var index = (int)tag;
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

        public bool IsTagSet(ModifierTag tag)
        {
            var index = (int)tag;
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

        public bool IsSubsetOf(in ModifierTagLookup other)
        {
            unsafe
            {
                fixed (int* ptrA = &f0, ptrB = &other.f0)
                {
                    for (int i = 0; i < NumParts; i++)
                    {
                        if ((*(ptrA + i) & *(ptrB + i)) != *(ptrA + i))
                            return false;
                    }
                    return true;
                }
            }
        }

        public bool HasAnyTagsSet()
        {
            unsafe
            {
                fixed (int* ptr = &f0)
                {
                    for (int i = 0; i < NumParts; i++)
                    {
                        if (*(ptr + i) != 0)
                            return true;
                    }
                    return false;
                }
            }
        }

        public void SetTagsFrom(in ModifierTagLookup other)
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

        public void UnsetTagsFrom(in ModifierTagLookup other)
        {
            unsafe
            {
                fixed (int* ptrA = &f0, ptrB = &other.f0)
                {
                    for (int i = 0; i < NumParts; i++)
                    {
                        *(ptrA + i) &= (int)~*(ptrB + i);
                    }
                }
            }
        }
    }
}