

namespace LukeyB.DeepStats.User
{
    public static class DeepStatsConstants
    {
        // Length of StatTypes list
        public const int NumStatTypes = 14;

        // Length of ModifierScalers list
        public const int NumModifierScalers = 3;

        // Length of ModifierTags list
        public const int NumModifierTags = 5;

        // Number of ways a stat can be modified, used to build arrays to track modifications
        public const int NumModifyTypes = 4;   // four sources to a stat: add, sumMultiply, productMultiply, conversion from another stat

        // Offset of final modifier types in the enum, used correct access to index in final modification array
        public const int FinalModifierTypeOffset = 1000;
    }
}