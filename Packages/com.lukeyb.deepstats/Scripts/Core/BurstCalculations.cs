using System;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Enums;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace LukeyB.DeepStats.Core
{
    [BurstCompile]
    public static class BurstCalculations
    {
        private static bool ModifierTagsPass(in ModifierTagLookup modifierTags, in ModifierTagLookup instanceTags)
        {
            return modifierTags.IsSubsetOf(instanceTags);
        }

        private static float2 GetDynamicModifierValue(in DeepModifier modifier, in ModifierTagLookup selfTags, in NativeArray<float> selfScalers, in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            // make sure all tags pass
            // if self/target tags hasnt been passed in but its a requirement, fail
            if (!ModifierTagsPass(modifier.SelfTags, selfTags) ||
                !ModifierTagsPass(modifier.TargetTags, targetTags))
            {
                if (Unity.Burst.CompilerServices.Hint.Unlikely(modifier.ModifierType == ModifierType.ProductMultiply))
                {
                    return 1f;
                }
                else
                {
                    return 0f;
                }
            }

            var modifierScaling = 1f;
            if (modifier.ModifierScalingSource != 0)
            {
                modifierScaling = 0f;
                if (selfScalers.Length > 0 && (modifier.ModifierScalingSource & DynamicSource.Self) != 0)
                {
                    modifierScaling += selfScalers[(int)modifier.ModifierScalerType];
                }
                if (targetScalers.Length > 0 && (modifier.ModifierScalingSource & DynamicSource.Target) != 0)
                {
                    modifierScaling += targetScalers[(int)modifier.ModifierScalerType];
                }

                modifierScaling = math.clamp(modifierScaling, modifier.ScalingClamp.x, modifier.ScalingClamp.y);
            }

            if (Unity.Burst.CompilerServices.Hint.Unlikely(modifier.ModifierType == ModifierType.ProductMultiply))
            {
                // Product Multiplies are usually set as 1.5 x ( + 50%) or 0.5x (- 50%), eg. they operate around 1.
                // Don't scale that 1 component, assume that if someone wants to scale a modifier in and out they don't want to flatten the entire stat if the scaler is 0
                var remainder = modifier.ModifyValue - 1;
                var modifierValue = 1f + modifierScaling * remainder;
                return modifierValue;
            }
            else
            {
                var modifierValue = modifierScaling * modifier.ModifyValue;
                return modifierValue;
            }
        }

        public static int ModIndex(in StatType stat, in ModifierType modifyType)
        {
            var index = (int)modifyType * DeepStatsConstants.NumStatTypes + (int)stat;
            return index;
        }

        private static int FinalModIndex(in StatType stat, in ModifierType modifyType)
        {
            var index = ((int)modifyType - 1000) * DeepStatsConstants.NumStatTypes + (int)stat;
            return index;
        }

        private static void CombineStatModifications(in NativeArray<float2> source, ref NativeArray<float2> target)
        {
            var productMultiplyStart = (int)ModifierType.ProductMultiply * DeepStatsConstants.NumStatTypes;
            var productMultiplyEnd = productMultiplyStart + DeepStatsConstants.NumStatTypes;

            for (var i = 0; i < DeepStatsConstants.NumStatTypes * DeepStatsConstants.NumModifyTypes; i++)
            {
                if (i >= productMultiplyStart && i < productMultiplyEnd)
                {
                    target[i] *= source[i];
                }
                else
                {
                    target[i] += source[i];
                }
            }
        }

        [BurstCompile]
        public static void AddUnprocessedValuesAsBase(in NativeArray<float2> source, ref NativeArray<float2> target)
        {
            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                target[ModIndex((StatType)i, ModifierType.Add)] += source[i];
            }
        }

        private static void AccumulateModifier(ref NativeArray<float2> modifications, in StatType targetStat, in ModifierType modifyType, float2 modifierValue)
        {
            var index = ModIndex(targetStat, modifyType);
            if (Unity.Burst.CompilerServices.Hint.Unlikely(modifyType == ModifierType.ProductMultiply))
            {
                modifications[index] *= modifierValue;
            }
            else
            {
                modifications[index] += modifierValue;
            }
        }

        private static void AccumulateFinalModifier(ref NativeArray<float2> modifications, in StatType targetStat, in ModifierType modifyType, float modifierValue)
        {
            var index = FinalModIndex(targetStat, modifyType);
            if (Unity.Burst.CompilerServices.Hint.Unlikely(modifyType == ModifierType.FinalProductMultiply))
            {
                modifications[index] *= modifierValue;
            }
            else
            {
                modifications[index] += modifierValue;
            }
        }

        private static void UpdateCachedConstantValues(in NativeList<DeepModifier> ConstantModifiers, ref NativeArray<float2> StatModifications)
        {
            // all sum multiplies and product multiplies need to start at 1
            var productMultiplyStart = (int)ModifierType.ProductMultiply * DeepStatsConstants.NumStatTypes;
            var productMultiplyEnd = productMultiplyStart + DeepStatsConstants.NumStatTypes;

            // reset modifications to initial state
            for (var i = 0; i < StatModifications.Length; i++)
            {
                StatModifications[i] = 0f;
            }
            // stat modifiers are grouped by modify type, start at the index of sum multiply, and end at the index of the last product multiply
            for (var i = productMultiplyStart; i < productMultiplyEnd; i++)
            {
                StatModifications[i] = 1f;
            }

            for (var i = 0; i < ConstantModifiers.Length; i++)
            {
                var mod = ConstantModifiers[i];
                var modifierValue = mod.ModifyValue;

                AccumulateModifier(ref StatModifications, mod.TargetStat, mod.ModifierType, modifierValue);
            }
        }

        private static void UpdateCachedSelfTaggedValues(in NativeList<DeepModifier> SelfDynamicModifiers, ref NativeArray<float2> StatModifications, in ModifierTagLookup selfTags, in NativeArray<float> selfScalers, in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            // product multiplies need to start at 1
            var productMultiplyStart = (int)ModifierType.ProductMultiply * DeepStatsConstants.NumStatTypes;
            var productMultiplyEnd = productMultiplyStart + DeepStatsConstants.NumStatTypes;

            // reset modifications to initial state
            for (var i = 0; i < StatModifications.Length; i++)
            {
                StatModifications[i] = 0f;
            }
            // stat modifiers are grouped by modify type, start at the index of product multiply, and end at the index of the last product multiply
            for (var i = productMultiplyStart; i < productMultiplyEnd; i++)
            {
                StatModifications[i] = 1f;
            }

            AddDynamicValues(SelfDynamicModifiers, selfTags, selfScalers, targetTags, targetScalers, ref StatModifications);
        }

        private static void AddDynamicValues(in NativeList<DeepModifier> DynamicModifiers,
            in ModifierTagLookup selfTags, in NativeArray<float> selfScaling, in ModifierTagLookup targetTags, in NativeArray<float> targetScaling,
            ref NativeArray<float2> StatModifications)
        {
            for (var i = 0; i < DynamicModifiers.Length; i++)
            {
                var mod = DynamicModifiers[i];
                var modifierValue = GetDynamicModifierValue(mod, selfTags, selfScaling, targetTags, targetScaling);

                AccumulateModifier(ref StatModifications, mod.TargetStat, mod.ModifierType, modifierValue);
            }
        }

        [BurstCompile]
        public static void AccumulateBasicModifiers(ref DeepStatsCollections _collections, in ModifierTagLookup selfTags, in NativeArray<float> selfScalers, in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            if (_collections._constantsStale)
            {
                UpdateCachedConstantValues(_collections._constantModifiers, ref _collections._cachedConstantModifications);
                _collections._constantsStale = false;
            }
            _collections._cachedConstantModifications.CopyTo(_collections._modifications);

            if (_collections._selfTaggedModsStale)
            {
                UpdateCachedSelfTaggedValues(_collections._selfTaggedModifiers, ref _collections._cachedSelfTaggedModifications, selfTags, selfScalers, targetTags, targetScalers);
                _collections._selfTaggedModsStale = false;
            }

            CombineStatModifications(_collections._cachedSelfTaggedModifications, ref _collections._modifications);

            AddDynamicValues(_collections._dynamicModifiers,
                selfTags, selfScalers, targetTags, targetScalers,
                ref _collections._modifications);
        }

        private static float2 ReadStatModifierValues(in StatType statType, in NativeArray<StatType> parentStats, in ModifierType modifyType, in NativeArray<float2> StatModifications)
        {
            float2 total;
            if (Unity.Burst.CompilerServices.Hint.Unlikely(modifyType == ModifierType.ProductMultiply))
            {
                total = new float2(1, 1);
            }
            else
            {
                total = float2.zero;
            }

            var st = statType;
            while (true)
            {
                if (modifyType == ModifierType.ProductMultiply)
                {
                    total *= StatModifications[ModIndex(st, modifyType)];
                }
                else
                {
                    total += StatModifications[ModIndex(st, modifyType)];
                }

                // move onto parent stat if it exists
                st = parentStats[(int)st];
                if ((int)st == -1)
                {
                    break;
                }
            }

            return total;
        }

        private static void AccumulateUnappliedStatModifiers(in StatType statType, in NativeArray<StatType> parentStats, ref StatTypeGroup collectedModifiers, in NativeArray<float2> StatModifications, ref float2 sumMultiply, ref float2 productMultiply)
        {
            var st = statType;
            while (true)
            {
                if (!collectedModifiers.IsStatSet(st))
                {
                    productMultiply *= StatModifications[ModIndex(st, ModifierType.ProductMultiply)];
                    sumMultiply += StatModifications[ModIndex(st, ModifierType.SumMultiply)];
                    collectedModifiers.SetStat(st, true);
                }

                // move onto parent stat if it exists
                st = parentStats[(int)st];
                if ((int)st == -1)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Runs Before any PostProcessing.
        /// Applies any Target Modifier conversions and AddedAs / ConvertedTo modifiers.
        /// Calculates initial Final Value using accumulated Modifiers.
        /// </summary>
        [BurstCompile]
        public static void GetUnprocessedValues(
            in NativeList<DeepModifier> ModifierConversions, in NativeList<DeepModifier> DependentModifiers,
            in ModifierTagLookup selfTags, in NativeArray<float> selfScaling, in ModifierTagLookup targetTags, in NativeArray<float> targetScaling,
            ref NativeArray<float2> totalConversionPerStat, ref NativeList<TotalDependent> totalDependent, in NativeArray<StatType> parentStats,
            ref NativeArray<float2> StatModifications, ref NativeArray<float2> UnprocessedValues)
        {
            AddModifierToTargetStat(ModifierConversions, selfTags, selfScaling, targetTags, targetScaling, ref StatModifications);

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                totalConversionPerStat[i] = 0;
            }
            
            ApplyStatConversions(DependentModifiers, selfTags, selfScaling, targetTags, targetScaling, ref totalConversionPerStat, ref totalDependent, parentStats, ref StatModifications, ref UnprocessedValues);

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                var converted = totalConversionPerStat[i];

                // cannot convert more than 100%
                if (converted.x > 1f)
                {
                    converted.x = 1;
                }
                if (converted.y > 1f)
                {
                    converted.y = 1;
                }

                // get the initial final value before any conversion
                var st = (StatType)i;
                var added = ReadStatModifierValues(st, parentStats, ModifierType.Add, StatModifications);
                var sumMulti = ReadStatModifierValues(st, parentStats, ModifierType.SumMultiply, StatModifications);
                var productMulti = ReadStatModifierValues(st, parentStats, ModifierType.ProductMultiply, StatModifications);
                var addedAndConverted = ReadStatModifierValues(st, parentStats, ModifierType.AddedAs, StatModifications);

                var initialValue = (added * (1 + sumMulti) * productMulti + addedAndConverted) * (1 - converted);
                UnprocessedValues[i] = initialValue;
            }
        }

        /// <summary>
        /// Runs after PostProcessing 1
        /// Applies any 'Final' Type Modifiers to the Final Values
        /// </summary>
        [BurstCompile]
        public static void UpdateFinalValues(in NativeList<DeepModifier> FinalModifiers,
            in ModifierTagLookup selfTags, in NativeArray<float> selfScaling, in ModifierTagLookup targetTags, in NativeArray<float> targetScaling,
            ref NativeArray<float2> finalModifications, ref NativeArray<float2> finalValues)
        {
            // all sum multiplies and product multiplies need to start at 1
            var sumMultiplyStart = (int)ModifierType.SumMultiply * DeepStatsConstants.NumStatTypes;
            var productMultiplyEnd = sumMultiplyStart + DeepStatsConstants.NumStatTypes * 2;

            // reset modifications to initial state
            for (var i = 0; i < finalModifications.Length; i++)
            {
                finalModifications[i] = 0f;
            }
            // stat modifiers are grouped by modify type, start at the index of product multiply, and end at the index of the last product multiply
            for (var i = sumMultiplyStart; i < productMultiplyEnd; i++)
            {
                finalModifications[i] = 1f;
            }

            for (var i = 0; i < FinalModifiers.Length; i++)
            {
                var mod = FinalModifiers[i];
                var modifierValue = GetDynamicModifierValue(mod, selfTags, selfScaling, targetTags, targetScaling);

                AccumulateFinalModifier(ref finalModifications, mod.TargetStat, mod.ModifierType, modifierValue.x);
            }

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                var st = (StatType)i;

                var initialFinalValue = finalValues[i];

                var added = finalModifications[FinalModIndex(st, ModifierType.FinalAdd)];
                var sumMulti = finalModifications[FinalModIndex(st, ModifierType.FinalSumMultiply)];
                var productMulti = finalModifications[FinalModIndex(st, ModifierType.FinalProductMultiply)];

                finalValues[i] = (initialFinalValue + added) * sumMulti * productMulti;
            }
        }

        private static void AddModifierToTargetStat(in NativeList<DeepModifier> ModifierConversions,
            in ModifierTagLookup selfTags, in NativeArray<float> selfScaling, in ModifierTagLookup targetTags, in NativeArray<float> targetScaling,
            ref NativeArray<float2> StatModifications)
        {
            // iterate backwards so we don't accumulate already converted modifiers, which would stack them together
            for (var i = ModifierConversions.Length - 1; i >= 0; i--)
            {
                var mod = ModifierConversions[i];

                var dependentModScaling = GetDynamicModifierValue(mod, selfTags, selfScaling, targetTags, targetScaling);

                if ((mod.TargetModifyTypes & CopyableModifyType.BaseAdd) != 0)
                {
                    var addedMod = dependentModScaling * StatModifications[ModIndex(mod.DependentStat, ModifierType.Add)];
                    StatModifications[ModIndex(mod.TargetStat, ModifierType.Add)] += addedMod;
                }
                if ((mod.TargetModifyTypes & CopyableModifyType.SumMultiply) != 0)
                {
                    var sumMod = dependentModScaling * StatModifications[ModIndex(mod.DependentStat, ModifierType.SumMultiply)];
                    StatModifications[ModIndex(mod.TargetStat, ModifierType.SumMultiply)] += sumMod;
                }
                if ((mod.TargetModifyTypes & CopyableModifyType.ProductMultiply) != 0)
                {
                    var prodMod = dependentModScaling * (StatModifications[ModIndex(mod.DependentStat, ModifierType.ProductMultiply)] - 1);
                    StatModifications[ModIndex(mod.TargetStat, ModifierType.ProductMultiply)] += prodMod;
                }
            }
        }

        private static void ApplyStatConversions(in NativeList<DeepModifier> DependentModifiers,
            in ModifierTagLookup selfTags, in NativeArray<float> selfScaling, in ModifierTagLookup targetTags, in NativeArray<float> targetScaling,
            ref NativeArray<float2> totalConversionPerStat, ref NativeList<TotalDependent> totalDependent, in NativeArray<StatType> parentStats,
            ref NativeArray<float2> StatModifications, ref NativeArray<float2> UnprocessedValues)
        {
            if (DependentModifiers.Length == 0)
            {
                return;
            }

            totalDependent.Clear();

            // collect all dependent modifiers together by pairs of dependent / target stat type
            var dependentTypes = 0;
            var dependent = DependentModifiers[0].DependentStat;
            var target = DependentModifiers[0].TargetStat;
            var totalConversionFrom = new float2();
            var conversionToSum = new float2();
            var addedAsSum = new float2();

            for (var i = 0; i < DependentModifiers.Length; i++)
            {
                if (DependentModifiers[i].TargetStat != target || DependentModifiers[i].DependentStat != dependent)
                {
                    totalDependent.Add(new TotalDependent(dependent, target, conversionToSum, addedAsSum));
                    dependentTypes++;

                    target = DependentModifiers[i].TargetStat;
                    conversionToSum = 0;
                    addedAsSum = 0;
                }

                if (DependentModifiers[i].DependentStat != dependent)
                {
                    totalConversionPerStat[(int)dependent] = totalConversionFrom;

                    dependent = DependentModifiers[i].DependentStat;
                    totalConversionFrom = 0;
                }

                var modifierValue = GetDynamicModifierValue(DependentModifiers[i], selfTags, selfScaling, targetTags, targetScaling);
                switch (DependentModifiers[i].ModifierType)
                {
                    case ModifierType.ConvertedTo:
                        conversionToSum += modifierValue;
                        totalConversionFrom += modifierValue;
                        break;
                    case ModifierType.AddedAs:
                        addedAsSum += modifierValue;
                        break;

                }
            }

            if (conversionToSum.x + addedAsSum.x != 0 || conversionToSum.y + addedAsSum.y != 0)  // add the final sums from the end of the loop
            {
                totalDependent.Add(new TotalDependent(dependent, target, conversionToSum, addedAsSum));
                totalConversionPerStat[(int)dependent] = totalConversionFrom;
                dependentTypes++;
            }

            // now iterate the total dependents, accumulate modifiers along the conversion chain
            for (var i = 0; i < totalDependent.Length; i++)
            {
                var dependentStat = totalDependent[i].DependentStat;

                var added = ReadStatModifierValues(dependentStat, parentStats, ModifierType.Add, StatModifications);

                var appliedStatTypes = new StatTypeGroup();
                var sumMultiplies = float2.zero;
                var productMultiplies = new float2(1, 1);
                AccumulateUnappliedStatModifiers(dependentStat, parentStats, ref appliedStatTypes, StatModifications, ref sumMultiplies, ref productMultiplies);

                // iterate all conversions in case the same stat is converted multiple times
                for (var j = i; j < totalDependent.Length; j++)
                {
                    var td = totalDependent[j];

                    if ((int)td.DependentStat > (int)dependentStat)
                    {
                        // if we've iterated entirely past the dependent stat, then the conversion chain is broken
                        break;
                    }

                    if (dependentStat != td.DependentStat)
                    {
                        continue;
                    }

                    var conversionSum = totalConversionPerStat[(int)td.DependentStat];

                    // cannot convert more than 100%. If we go over, scale all conversions down.
                    var convertedToMultiplier = new float2(1f, 1f);
                    if (conversionSum.x > 1f)
                    {
                        convertedToMultiplier.x = 1 / conversionSum.x;
                    }
                    if (conversionSum.y > 1f)
                    {
                        convertedToMultiplier.y = 1 / conversionSum.y;
                    }

                    AccumulateUnappliedStatModifiers(td.TargetStat, parentStats, ref appliedStatTypes, StatModifications, ref sumMultiplies, ref productMultiplies);

                    var convertedBase = added * td.TotalConversion * convertedToMultiplier;
                    var addedBase = added * td.TotalAddedAs;

                    var total = (convertedBase + addedBase) * (1 + sumMultiplies) * productMultiplies;

                    AccumulateModifier(ref StatModifications, td.TargetStat, ModifierType.AddedAs, total);

                    added = convertedBase + addedBase;
                    dependentStat = td.TargetStat;
                }
            }
        }
    }
}
