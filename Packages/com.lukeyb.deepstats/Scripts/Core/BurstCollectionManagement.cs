using LukeyB.DeepStats.Enums;
using Unity.Burst;
using Unity.Collections;

namespace LukeyB.DeepStats.Core
{
    [BurstCompile]
    public static class BurstCollectionManagement
    {
        [BurstCompile]
        public static void AddModifier(ref DeepStatsCollections collections, in DeepModifier mod)
        {
            // figure out which collection this modifier belongs in
            if (IsDependentModifier(mod.ModifierType))
            {
                InsertSortedDependentThenTarget(mod, ref collections._dependentModifiers);
            }
            else if (mod.ModifierType == ModifierType.ModifiersAlsoApplyToStat)
            {
                InsertSortedStatThenModifierType(mod, ref collections._modsAlsoApplyToStat);  // dont change the sort order, calculation depends on it
            }
            else if (mod.ModifierType == ModifierType.ConvertSelfTags || mod.ModifierType == ModifierType.ConvertTargetTags)
            {
                collections._modsAlsoApplyToTags.Add(mod);
                AddNewMatchingTagModifier(ref collections, mod);
            }
            else if (mod.ModifierType == ModifierType.FinalAdd || mod.ModifierType == ModifierType.FinalSumMultiply || mod.ModifierType == ModifierType.FinalProductMultiply)
            {
                InsertSortedModifyType(mod, ref collections._finalModifiers);
            }
            else
            {
                if (IsConstantModifier(mod))
                {
                    InsertSortedModifyType(mod, ref collections._constantModifiers);
                    collections._constantsStale = true;
                }
                else
                {
                    if (ModOnlyDependsOnSelfTags(mod))
                    {
                        InsertSortedModifyType(mod, ref collections._selfTaggedModifiers);
                        collections._selfTaggedModsStale = true;
                    }
                    else
                    {
                        InsertSortedDynamicSources(mod, ref collections._dynamicModifiers);
                    }
                }
                CheckNewModifierForMatchingTags(ref collections, mod);
            }

            collections._finalValuesStale = true;
        }

        [BurstCompile]
        public static void RemoveModifier(ref DeepStatsCollections collections, in DeepModifier mod)
        {
            if (IsDependentModifier(mod.ModifierType))
            {
                RemoveModifier(ref collections._dependentModifiers, mod.ModifierIdentifier, mod.ModifierSource);
            }
            else if (mod.ModifierType == ModifierType.ModifiersAlsoApplyToStat)
            {
                RemoveModifier(ref collections._modsAlsoApplyToStat, mod.ModifierIdentifier, mod.ModifierSource);
            }
            else if (mod.ModifierType == ModifierType.ConvertSelfTags)
            {
                RemoveMatchingTagModifierChildren(ref collections, mod.ModifierIdentifier);
                RemoveModifier(ref collections._modsAlsoApplyToTags, mod.ModifierIdentifier, mod.ModifierSource);
            }
            else if (mod.ModifierType == ModifierType.FinalAdd || mod.ModifierType == ModifierType.FinalSumMultiply || mod.ModifierType == ModifierType.FinalProductMultiply)
            {
                RemoveModifier(ref collections._finalModifiers, mod.ModifierIdentifier, mod.ModifierSource);
            }
            else
            {
                if (IsConstantModifier(mod))
                {
                    RemoveModifier(ref collections._constantModifiers, mod.ModifierIdentifier, mod.ModifierSource);
                   collections._constantsStale = true;
                }
                else
                {
                    if (ModOnlyDependsOnSelfTags(mod))
                    {
                        RemoveModifier(ref collections._selfTaggedModifiers, mod.ModifierIdentifier, mod.ModifierSource);
                        collections._selfTaggedModsStale = true;
                    }
                    else
                    {
                        RemoveModifier(ref collections._dynamicModifiers, mod.ModifierIdentifier, mod.ModifierSource);
                    }
                }
            }

            collections._finalValuesStale = true;
        }

        private static void InsertSortedModifyType(in DeepModifier item, ref NativeList<DeepModifier> modifiers)
        {
            int index = -1;

            for (int i = 0; i < modifiers.Length; i++)
            {
                var lowerPriorityModify = (int)modifiers[i].ModifierType > (int)item.ModifierType;
                var samePriorityModify = modifiers[i].ModifierType == item.ModifierType;

                bool lowerPriorityStat = (int)modifiers[i].TargetStat > (int)item.TargetStat;

                if (lowerPriorityModify ||
                    (samePriorityModify && lowerPriorityStat))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {

                modifiers.Add(item);
            }
            else
            {
                modifiers.InsertRangeWithBeginEnd(index, index + 1);
                modifiers[index] = item;
            }
        }

        private static void InsertSortedStatThenModifierType(in DeepModifier item, ref NativeList<DeepModifier> modifiers)
        {
            int index = -1;

            for (int i = 0; i < modifiers.Length; i++)
            {
                bool lowerPriorityStat = (int)modifiers[i].DependentStat > (int)item.DependentStat;
                var samePriorityStat = (int)modifiers[i].DependentStat == (int)item.DependentStat;
                var lowerPriorityModify = (int)modifiers[i].ModifierType > (int)item.ModifierType;

                if (lowerPriorityStat || (samePriorityStat && lowerPriorityModify))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                modifiers.Add(item);
            }
            else
            {
                modifiers.InsertRangeWithBeginEnd(index, index + 1);
                modifiers[index] = item;
            }
        }

        private static void InsertSortedDependentThenTarget(in DeepModifier item, ref NativeList<DeepModifier> modifiers)
        {
            int index = -1;

            for (int i = 0; i < modifiers.Length; i++)
            {
                bool lowerPriorityStat = (int)modifiers[i].DependentStat > (int)item.DependentStat;
                var samePriorityStat = (int)modifiers[i].DependentStat == (int)item.DependentStat;
                var lowerPriorityTarget = (int)modifiers[i].TargetStat > (int)item.TargetStat;

                if (lowerPriorityStat || (samePriorityStat && lowerPriorityTarget))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                modifiers.Add(item);
            }
            else
            {
                modifiers.InsertRangeWithBeginEnd(index, index + 1);
                modifiers[index] = item;
            }
        }

        private static void InsertSortedDynamicSources(in DeepModifier item, ref NativeList<DeepModifier> modifiers)
        {
            int index = -1;

            var itemTagsSort = 0;
            if (item.TargetTags.HasAnyTagsSet())
            {
                itemTagsSort = itemTagsSort + 2;
            }
            if (item.SelfTags.HasAnyTagsSet())
            {
                itemTagsSort = itemTagsSort + 1;
            }

            for (int i = 0; i < modifiers.Length; i++)
            {
                var mod = modifiers[i];

                var lowerPriorityScaling = (int)mod.ModifierScalingSource > (int)item.ModifierScalingSource;
                var samePriorityScaling = (int)mod.ModifierScalingSource == (int)item.ModifierScalingSource;

                var modTagsSort = 0;
                if (mod.TargetTags.HasAnyTagsSet())
                {
                    modTagsSort = modTagsSort + 2;
                }
                if (mod.SelfTags.HasAnyTagsSet())
                {
                    modTagsSort = modTagsSort + 1;
                }

                var lowerPriorityTags = modTagsSort > itemTagsSort;
                var samePriorityTags = modTagsSort == itemTagsSort;

                var lowerPriorityModify = (int)mod.ModifierType > (int)item.ModifierType;
                var samePriorityModify = mod.ModifierType == item.ModifierType;

                bool lowerPriorityStat = (int)mod.TargetStat > (int)item.TargetStat;

                if (lowerPriorityScaling ||
                    (samePriorityScaling && lowerPriorityTags) ||
                    (samePriorityScaling && samePriorityTags && lowerPriorityModify) ||
                    (samePriorityScaling && lowerPriorityTags && samePriorityModify && lowerPriorityStat))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                modifiers.Add(item);
            }
            else
            {
                modifiers.InsertRangeWithBeginEnd(index, index + 1);
                modifiers[index] = item;
            }
        }

        private static bool RemoveModifier(ref NativeList<DeepModifier> modifiers, int modifierIdentifier, ModifierSource source)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i].ModifierIdentifier == modifierIdentifier && modifiers[i].ModifierSource == source)
                {
                    modifiers.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static void AddNewMatchingTagModifier(ref DeepStatsCollections collections, in DeepModifier matchingTagMod)
        {

            for (var i = 0; i < collections._constantModifiers.Length; i++)
            {
                AddModifierIfMatchingTags(collections._constantModifiers[i], matchingTagMod, ref collections);
            }
            for (var i = 0; i < collections._dynamicModifiers.Length; i++)
            {
                AddModifierIfMatchingTags(collections._dynamicModifiers[i], matchingTagMod, ref collections);
            }
            for (var i = 0; i < collections._selfTaggedModifiers.Length; i++)
            {
                AddModifierIfMatchingTags(collections._selfTaggedModifiers[i], matchingTagMod, ref collections);
            }
        }

        private static void CheckNewModifierForMatchingTags(ref DeepStatsCollections collections, in DeepModifier newMod)
        {
            for (var i = 0; i < collections._modsAlsoApplyToTags.Length; i++)
            {
                AddModifierIfMatchingTags(newMod, collections._modsAlsoApplyToTags[i], ref collections);
            }
        }

        private static void AddModifierIfMatchingTags(in DeepModifier modifier, in DeepModifier matchingTagMod, ref DeepStatsCollections collections)
        {
            if (modifier.ModifierSource == ModifierSource.FromAddedTagsModifier)
            {
                return;
            }

            if (modifier.TargetStat != matchingTagMod.TargetStat)
            {
                return;
            }

            if (!ModifyTypesMatch(matchingTagMod.TargetModifyTypes, modifier.ModifierType))
            {
                return;
            }

            bool compareModSelf = matchingTagMod.ModifierType == ModifierType.ConvertSelfTags;

            // on the matchingTagMod, self tags are the required matching tags and target tags are the replacement
            if (compareModSelf)
            {
                if (!matchingTagMod.SelfTags.IsSubsetOf(modifier.SelfTags))
                {
                    return; // if mod doesnt have the required tags
                }
                if (matchingTagMod.TargetTags.IsSubsetOf(modifier.SelfTags))
                {
                    return; // if mod already has the new tags, dont copy it
                }
            }
            else
            {
                if (!matchingTagMod.SelfTags.IsSubsetOf(modifier.TargetTags))
                {
                    return;
                }
                if (matchingTagMod.TargetTags.IsSubsetOf(modifier.TargetTags))
                {
                    return; // if mod already has the new tags, dont copy it
                }
            }

            // second two tags are what gets converted to
            var matchedMod = modifier;
            matchedMod.ModifierSource = ModifierSource.FromAddedTagsModifier;
            matchedMod.ModifierIdentifier = matchingTagMod.ModifierIdentifier;  // do this so when we remove the tag mod, we can quickly find any children of it and remove those too
            matchedMod.ModifyValue = modifier.ModifyValue * matchingTagMod.ModifyValue;

            if (compareModSelf)
            {
                matchedMod.SelfTags.UnsetTagsFrom(matchingTagMod.SelfTags);
                matchedMod.SelfTags.SetTagsFrom(matchingTagMod.TargetTags);
            }
            else
            {
                matchedMod.TargetTags.UnsetTagsFrom(matchingTagMod.SelfTags);
                matchedMod.TargetTags.SetTagsFrom(matchingTagMod.TargetTags);
            }

            AddModifier(ref collections, matchedMod);
        }

        private static void RemoveMatchingTagModifierChildren(ref DeepStatsCollections collections, int modifierIdentifier)
        {
            // search for any previously created modifiers which were a child of the matching tag modifier and remove them
            for (var i = collections._constantModifiers.Length - 1; i >= 0; i--)
            {
                if (collections._constantModifiers[i].ModifierIdentifier == modifierIdentifier && 
                    collections._constantModifiers[i].ModifierSource == ModifierSource.FromAddedTagsModifier) { collections._constantModifiers.RemoveAt(i); }
            }
            for (var i = collections._dynamicModifiers.Length - 1; i >= 0; i--)
            {
                if (collections._dynamicModifiers[i].ModifierIdentifier == modifierIdentifier && 
                    collections._dynamicModifiers[i].ModifierSource == ModifierSource.FromAddedTagsModifier) { collections._dynamicModifiers.RemoveAt(i); }
            }
            for (var i = collections._selfTaggedModifiers.Length - 1; i >= 0; i--)
            {
                if (collections._selfTaggedModifiers[i].ModifierIdentifier == modifierIdentifier && 
                    collections._selfTaggedModifiers[i].ModifierSource == ModifierSource.FromAddedTagsModifier) { collections._selfTaggedModifiers.RemoveAt(i); }
            }
        }

        private static bool IsDependentModifier(ModifierType modType)
        {
            if (modType == ModifierType.AddedAs || modType == ModifierType.ConvertedTo)
            {
                return true;
            }
            return false;
        }

        private static bool IsConstantModifier(in DeepModifier mod)
        {
            if (mod.ModifierScalingSource == 0 && !mod.SelfTags.HasAnyTagsSet() && !mod.TargetTags.HasAnyTagsSet())
            {
                return true;
            }
            return false;
        }

        private static bool ModOnlyDependsOnSelfTags(in DeepModifier mod)
        {
            if (mod.ModifierScalingSource == DynamicSource.None && !mod.TargetTags.HasAnyTagsSet())
            {
                return true;
            }
            return false;
        }

        private static bool ModifyTypesMatch(CopyableModifyType required, ModifierType target)
        {
            if (target == ModifierType.Add && ((required & CopyableModifyType.BaseAdd) != 0) ||
                target == ModifierType.SumMultiply && ((required & CopyableModifyType.SumMultiply) != 0) ||
                target == ModifierType.ProductMultiply && ((required & CopyableModifyType.ProductMultiply) != 0))
            {
                return true;
            }
            return false;
        }
    }
}
