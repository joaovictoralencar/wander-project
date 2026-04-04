using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.Enums;
using LukeyB.DeepStats.ScriptableObjects;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    public class StatInstancerBursted : MonoBehaviour
    {
        public int NumInstances;
        public int ConstantModsPerInstance;
        public int DynamicModsPerInstance;
        public int DependentModsPerInstance;

        public DeepStatsConfigurationSO Config;
        public List<EditorDeepModifier> GeneratedConstants;
        public List<EditorDeepModifier> GeneratedDynamics;
        public List<EditorDeepModifier> GeneratedDependents;

        public DeepStatsInstance[] Stats;

        private GUIStyle style;
        private WaitForSeconds countWait;
        private float count;
        private Stopwatch _sw = new Stopwatch();
        private Queue<float> fpsQueue = new Queue<float>(10);
        private Queue<float> timeQueue = new Queue<float>(10);
        bool updateTime = false;

        private float _initialiseTime;

        void Awake()
        {
            style = new GUIStyle();
            countWait = new WaitForSeconds(0.1f);

            Stats = new DeepStatsInstance[NumInstances];

            GeneratedConstants = new List<EditorDeepModifier>();
            GeneratedDynamics = new List<EditorDeepModifier>();
            GeneratedDependents = new List<EditorDeepModifier>();

            _sw.Start();

            for (var i = 0; i < NumInstances; i++)
            {
                Stats[i] = new DeepStatsInstance();

                for (var k = 0; k < DeepStatsConstants.NumModifierScalers; k++)
                {
                    Stats[i].SetScaler((ModifierScaler)k, k % 2 + 0.5f);
                }

                for (var j = 0; j < ConstantModsPerInstance; j++)
                {
                    var mod = new DeepModifier()
                    {
                        ModifierType = (ModifierType)(j % 3),
                        ModifyValue = (j % 4) + 1,
                        TargetStat = (StatType)(j % DeepStatsConstants.NumStatTypes),
                        DependentStat = (StatType)(j % DeepStatsConstants.NumStatTypes),
                    };

                    Stats[i].AddModifier(mod);
                    if (i == 0)
                    {
                        var targetSO = Config.StatTypes.First(so => so.name == mod.TargetStat.ToString());
                        var dependentSO = Config.StatTypes.First(so => so.name == mod.DependentStat.ToString());

                        var editorMod = new EditorDeepModifier(mod.ModifierType, mod.ModifyValue, mod.MinMaxValues, targetSO, dependentSO, mod.TargetModifyTypes, mod.ModifierScalingSource, null, new ModifierTagSO[0], new ModifierTagSO[0]);
                        GeneratedConstants.Add(editorMod);
                    }
                }

                for (var j = 0; j < DynamicModsPerInstance; j++)
                {
                    var selfTags = new ModifierTagLookup();
                    var targetTags = new ModifierTagLookup();

                    // make tags alternate
                    if (j % 3 == 0)
                    {
                        selfTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);
                        targetTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);
                    }
                    else if (j % 2 == 0)
                    {
                        selfTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);
                    }
                    else
                    {
                        targetTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);
                    }

                    var mod = new DeepModifier()
                    {
                        ModifierType = (ModifierType)(j % 3),
                        ModifyValue = (j % 4) + 2,
                        TargetStat = (StatType)(j % DeepStatsConstants.NumStatTypes),
                        DependentStat = (StatType)(j % DeepStatsConstants.NumStatTypes),
                        ModifierScalingSource = (DynamicSource)(3f - Mathf.Floor(Mathf.Sqrt(Random.Range(0, 16)))),   // looks ugly, but basically bias the scaling source towards None so that we can cache a realistic number of these modifiers
                        ModifierScalerType = (ModifierScaler)(j % DeepStatsConstants.NumModifierScalers),
                        SelfTags = selfTags,
                        TargetTags = targetTags,
                    };

                    Stats[i].AddModifier(mod);
                    if (i == 0)
                    {
                        var targetSO = Config.StatTypes.First(so => so.name == mod.TargetStat.ToString());
                        var dependentSO = Config.StatTypes.First(so => so.name == mod.DependentStat.ToString());
                        var scaling = Config.ModifierScalers.First(so => so.name == mod.ModifierScalerType.ToString());
                        var selfSOs = Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => selfTags.IsTagSet((ModifierTag)ind)).Select(ind => Config.ModifierTags.First(so => so.name == ((ModifierTag)ind).ToString())).ToArray();
                        var targetSOs = Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).Select(ind => Config.ModifierTags.First(so => so.name == ((ModifierTag)ind).ToString())).ToArray();

                        if (selfSOs.Length == 0 && targetSOs.Length == 0)
                        {
                            throw new System.Exception();
                        }

                        var editorMod = new EditorDeepModifier(mod.ModifierType, mod.ModifyValue, mod.MinMaxValues, targetSO, dependentSO, mod.TargetModifyTypes, mod.ModifierScalingSource, scaling, selfSOs, targetSOs);
                        GeneratedDynamics.Add(editorMod);
                    }
                }

                for (var j = 0; j < DependentModsPerInstance; j++)
                {
                    // only use up to the first 4 stat types for conversions, otherwise you end up with big chains of conversions that are completely unrealistic
                    var maxStatIndex = Mathf.Min(DeepStatsConstants.NumStatTypes, 3);
                    var dependentStat = j % maxStatIndex;    // last stat type cannot be converted to anything

                    // target stat cannot be less or equal to dependent stat
                    var targetStat = dependentStat + (int)(j * 1.15f) % (maxStatIndex - dependentStat) + 1;  

                    var selfTags = new ModifierTagLookup();
                    var targetTags = new ModifierTagLookup();

                    // make tags alternate
                    selfTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);
                    targetTags.SetTag((ModifierTag)(j % DeepStatsConstants.NumModifierTags), true);

                    var mod = new DeepModifier()
                    {
                        ModifierType = (ModifierType)(j % 4) + (int)ModifierType.AddedAs,
                        ModifyValue = ((j % 4) + 2) / 5f,
                        TargetStat = (StatType)targetStat,
                        DependentStat = (StatType)dependentStat,
                        ModifierScalingSource = (DynamicSource)Random.Range(0, 4),
                        ModifierScalerType = (ModifierScaler)(j % DeepStatsConstants.NumModifierScalers),
                        TargetModifyTypes = (CopyableModifyType)(1 << (j % 3)),
                        SelfTags = selfTags,
                        TargetTags = targetTags,
                    };

                    Stats[i].AddModifier(mod);

                    if (i == 0)
                    {
                        var targetSO = Config.StatTypes.First(so => so.name == mod.TargetStat.ToString());
                        var dependentSO = Config.StatTypes.First(so => so.name == mod.DependentStat.ToString());
                        var scaling = Config.ModifierScalers.First(so => so.name == mod.ModifierScalerType.ToString());
                        var selfSOs = Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => selfTags.IsTagSet((ModifierTag)ind)).Select(ind => Config.ModifierTags.First(so => so.name == ((ModifierTag)ind).ToString())).ToArray();
                        var targetSOs = Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).Select(ind => Config.ModifierTags.First(so => so.name == ((ModifierTag)ind).ToString())).ToArray();

                        var editorMod = new EditorDeepModifier(mod.ModifierType, mod.ModifyValue, mod.MinMaxValues, targetSO, dependentSO, mod.TargetModifyTypes, mod.ModifierScalingSource, scaling, selfSOs, targetSOs);
                        GeneratedDependents.Add(editorMod);
                    }
                }
            }

            _sw.Stop();

            _initialiseTime = _sw.ElapsedMilliseconds;
        }

        private IEnumerator Start()
        {
            GUI.depth = 2;
            while (true)
            {
                updateTime = true;
                count = 1f / Time.unscaledDeltaTime;
                fpsQueue.Enqueue(count);
                if (fpsQueue.Count > 10) { fpsQueue.Dequeue(); }
                yield return countWait;
            }
        }

        private void Update()
        {
            _sw.Restart();
            for (var i = 0; i < NumInstances; i++)
            {
                var target = i + 1 < NumInstances ? Stats[i + 1] : Stats[0];

                Stats[i].UpdateFinalValues(target);
            }
            _sw.Stop();

            if (updateTime)
            {
                updateTime = false;
                timeQueue.Enqueue(_sw.ElapsedMilliseconds);
                if (timeQueue.Count > 10) { timeQueue.Dequeue(); }
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < NumInstances; i++)
            {
                Stats[i].Dispose();
            }
        }

        private void OnGUI()
        {
            style.fontSize = (int)(Screen.width / 40f);
            var averageMs = timeQueue.Average();
            var averageFps = fpsQueue.Average();

            var description = @"FPS: " + Mathf.Round(averageFps) + @"

Took " + Mathf.Round(_initialiseTime) + @" ms to build:
" + NumInstances + @" DeepStat instances with 
- " + ConstantModsPerInstance + " Constant Modifiers (" + ConstantModsPerInstance + @" could be cached)
- " + DynamicModsPerInstance + " Scaled and Tagged Modifiers (" + Stats[0].CachedCount + @" could be cached)        
- " + DependentModsPerInstance + @" Stat Conversion Modifiers

Recalculating all DeepStats instances took " + (averageMs).ToString("F2") + @" milliseconds";

            GUI.Label(new Rect(5, 40, 300, 50), description, style);
        }
    }

}
