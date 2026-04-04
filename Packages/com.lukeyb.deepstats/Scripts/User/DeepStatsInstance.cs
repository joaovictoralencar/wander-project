using System;
using System.Collections.Generic;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.Enums;
using LukeyB.DeepStats.GameObjects;
using Unity.Collections;
using Unity.Mathematics;

namespace LukeyB.DeepStats.User
{
    public class DeepStatsInstance
    {
        private DeepStatsCollections _collections = DeepStatsCollections.New();
        private List<DeepStatsInstance> _addedStatSources = new List<DeepStatsInstance>();
        private Action SourceHasChanged;

        private ModifierScalers _scalers = new ModifierScalers();
        private ModifierTags _tags = new ModifierTags();

        private NativeArray<float2> RawValues = new NativeArray<float2>(DeepStatsConstants.NumStatTypes, Allocator.Persistent);
        private NativeArray<float2> FinalValues = new NativeArray<float2>(DeepStatsConstants.NumStatTypes, Allocator.Persistent);

        public int CachedCount { get { return _collections._selfTaggedModifiers.Length; } }
        public bool FinalValuesAreStale { get { return _collections._finalValuesStale; } }
        public ModifierIterator Modifiers { get; private set; }

        public DeepStatsInstance()
        {
            _tags.TagsChanged += SetSelfTaggedModsStale;

            Modifiers = new ModifierIterator();
            Modifiers.AddCollection(_collections._constantModifiers);
            Modifiers.AddCollection(_collections._dynamicModifiers);
            Modifiers.AddCollection(_collections._selfTaggedModifiers);
            Modifiers.AddCollection(_collections._modsAlsoApplyToStat);
            Modifiers.AddCollection(_collections._modsAlsoApplyToTags);
            Modifiers.AddCollection(_collections._dependentModifiers);
            Modifiers.AddCollection(_collections._finalModifiers);
        }
        public void AddModifier(EditorDeepModifier mod)
        {
            AddModifier(mod.Value);
        }

        public void AddModifier(DeepModifier mod)
        {
            AddModifierInternal(mod, ModifierSource.Self);
        }

        /// <summary>
        /// Dont call this, use AddModifier instead
        /// </summary>
        public void AddModifierInternal(DeepModifier mod, ModifierSource isOwned)
        {
            mod.ModifierSource = isOwned;

            BurstCollectionManagement.AddModifier(ref _collections, mod);

            ModifiersChanged();
        }

        public void RemoveModifier(EditorDeepModifier mod)
        {
            RemoveModifier(mod.Value);
        }

        public void RemoveModifier(DeepModifier mod)
        {
            RemoveModifierInternal(mod, ModifierSource.Self);
        }

        /// <summary>
        /// Dont call this, use RemoveModifier instead
        /// </summary>
        public void RemoveModifierInternal(DeepModifier mod, ModifierSource source)
        {
            mod.ModifierSource = source;

            BurstCollectionManagement.RemoveModifier(ref _collections, mod);

            ModifiersChanged();
        }

        public void ClearAllModifiers()
        {
            foreach (var d in Modifiers.OwnedModifiers)
            {
                RemoveModifierInternal(d, ModifierSource.Self);
            }
        }

        public void AddAddedStatsSource(DeepStatsInstance stats)
        {
            _addedStatSources.Add(stats);
            stats.SourceHasChanged += FinalValuesStale;
            FinalValuesStale();
        }

        public void RemoveAddedStatsSource(DeepStatsInstance stats)
        {
            _addedStatSources.Remove(stats);
            stats.SourceHasChanged -= FinalValuesStale;
            FinalValuesStale();
        }

        private void FinalValuesStale()
        {
            _collections._finalValuesStale = true;
        }

        private void ModifiersChanged()
        {
            Modifiers.SetCountsStale();
            SourceHasChanged?.Invoke();
        }

        private void SetSelfTaggedModsStale()
        {
            _collections._selfTaggedModsStale = true;
            FinalValuesStale();
        }

        public void UpdateFinalValues(DeepStatsInstance target)
        {
            var targetTags = target != null ? target._tags.Tags : DeepStatsManager.DefaultTags;
            var targetScalers = target != null ? target._scalers.Array : DeepStatsManager.DefaultScalers;

            UpdateFinalValuesInternal(_tags.Tags, _scalers.Array, targetTags, targetScalers);
        }

        private void UpdateFinalValuesInternal(in ModifierTagLookup selfTags, in NativeArray<float> selfScalers, in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            CalculateUnprocessedValues(selfTags, selfScalers, targetTags, targetScalers);

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                FinalValues[i] = RawValues[i];
            }

            BurstCalculations.UpdateFinalValues(_collections._finalModifiers, selfTags, selfScalers, targetTags, targetScalers, ref _collections._finalModifications, ref FinalValues);

            _collections._finalValuesStale = false;
        }

        private void CalculateUnprocessedValues(in ModifierTagLookup selfTags, in NativeArray<float> selfScalers, in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            BurstCalculations.AccumulateBasicModifiers(ref _collections, selfTags, selfScalers, targetTags, targetScalers);

            // added stat sources are calculated to final values, then added as base values
            for (var i = 0; i < _addedStatSources.Count; i++)
            {
                _addedStatSources[i].CalculateChildUnprocessedValues(targetTags, targetScalers);    // children use their own tags and scalers

                BurstCalculations.AddUnprocessedValuesAsBase(_addedStatSources[i].RawValues, ref _collections._modifications);
            }

            BurstCalculations.GetUnprocessedValues(_collections._modsAlsoApplyToStat, _collections._dependentModifiers,
                selfTags, selfScalers, targetTags, targetScalers,
                ref DeepStatsManager.TotalConversionPerStat, ref DeepStatsManager.TotalDependent, in DeepStatsManager.ParentStats,
                ref _collections._modifications, ref RawValues);
        }

        private void CalculateChildUnprocessedValues(in ModifierTagLookup targetTags, in NativeArray<float> targetScalers)
        {
            CalculateUnprocessedValues(_tags.Tags, _scalers.Array, targetTags, targetScalers);
        }

        public float2 GetRawValue(StatType type)
        {
            return RawValues[(int)type];
        }

        public float2 GetFinalRange(StatType type)
        {
            return FinalValues[(int)type];
        }

        public float GetFinalValue(StatType type)
        {
            return UnityEngine.Random.Range(FinalValues[(int)type].x, FinalValues[(int)type].y);
        }

        public float this[StatType type]
        {
            get
            {
                return GetFinalValue(type);

            }
        }

        /// <summary>
        /// Gets the latest calculated total modifier to a stat
        /// </summary>
        /// <param name="statType">The stat type to retrieve</param>
        /// <param name="modifyType">The modify type to retrieve</param>
        /// <returns></returns>
        public float2 GetRawModifierTotal(StatType statType, ModifierType modifyType)
        {
            return _collections._modifications[BurstCalculations.ModIndex(statType, modifyType)];
        }

        public void SetScaler(ModifierScalerSO scaler, float value)
        {
            SetScaler(scaler.EnumValue, value);
        }

        public void SetScaler(ModifierScaler scaler, float value)
        {
            _scalers[scaler] = value;
        }

        public void SetTag(ModifierTagSO tag, bool value)
        {
            SetTag(tag.EnumValue, value);
        }

        public void SetTag(ModifierTag tag, bool value)
        {
            _tags[tag] = value;
        }

        public void Dispose()
        {
            _collections.Dispose();

            _scalers.Dispose();

            RawValues.Dispose();
            FinalValues.Dispose();
        }
    }
} 