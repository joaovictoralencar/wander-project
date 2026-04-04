using System.Collections;
using System.Runtime.InteropServices;
using LukeyB.DeepStats.User;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LukeyB.DeepStats.Core
{
    public struct DeepStatsCollections
    {
        public NativeList<DeepModifier> _constantModifiers;
        public NativeList<DeepModifier> _dynamicModifiers;
        public NativeList<DeepModifier> _selfTaggedModifiers;
        public NativeList<DeepModifier> _modsAlsoApplyToStat;
        public NativeList<DeepModifier> _modsAlsoApplyToTags;
        public NativeList<DeepModifier> _dependentModifiers;
        public NativeList<DeepModifier> _finalModifiers;

        public NativeArray<float2> _cachedConstantModifications;
        public NativeArray<float2> _cachedSelfTaggedModifications;
        public NativeArray<float2> _modifications;
        public NativeArray<float2> _finalModifications;

        [MarshalAs(UnmanagedType.U1)] public bool _constantsStale;
        [MarshalAs(UnmanagedType.U1)] public bool _selfTaggedModsStale;
        [MarshalAs(UnmanagedType.U1)] public bool _finalValuesStale;

        public static DeepStatsCollections New()
        {
            var collections = new DeepStatsCollections();
            collections._constantModifiers = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._dynamicModifiers = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._selfTaggedModifiers = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._modsAlsoApplyToStat = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._modsAlsoApplyToTags = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._dependentModifiers = new NativeList<DeepModifier>(Allocator.Persistent);
            collections._finalModifiers = new NativeList<DeepModifier>(Allocator.Persistent);

            collections._cachedConstantModifications = new NativeArray<float2>(DeepStatsConstants.NumModifyTypes * DeepStatsConstants.NumStatTypes, Allocator.Persistent);
            collections._cachedSelfTaggedModifications = new NativeArray<float2>(DeepStatsConstants.NumModifyTypes * DeepStatsConstants.NumStatTypes, Allocator.Persistent);
            collections._modifications = new NativeArray<float2>(DeepStatsConstants.NumModifyTypes * DeepStatsConstants.NumStatTypes, Allocator.Persistent);
            collections._finalModifications = new NativeArray<float2>(DeepStatsConstants.NumModifyTypes * DeepStatsConstants.NumStatTypes, Allocator.Persistent);

            collections._constantsStale = true;
            collections._selfTaggedModsStale = true;
            collections._finalValuesStale = true;

            return collections;
        }

        public void Dispose()
        {
            _constantModifiers.Dispose();
            _dynamicModifiers.Dispose();
            _selfTaggedModifiers.Dispose();
            _modsAlsoApplyToStat.Dispose();
            _modsAlsoApplyToTags.Dispose();
            _dependentModifiers.Dispose();
            _finalModifiers.Dispose();

            _cachedConstantModifications.Dispose();
            _cachedSelfTaggedModifications.Dispose();
            _modifications.Dispose();
            _finalModifications.Dispose();
        }
    }
}
