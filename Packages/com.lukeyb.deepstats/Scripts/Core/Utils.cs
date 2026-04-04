using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using LukeyB.DeepStats.Enums;
using LukeyB.DeepStats.ScriptableObjects;
using LukeyB.DeepStats.User;

namespace LukeyB.DeepStats.Core
{
    public static class DeepStatsUtils
    {
        private static StringBuilder _sb = new StringBuilder();

        public static string GetStatModifierDescription(DeepModifier mod)
        {
            AddCoreDescription(mod.TargetStat.ToString(), mod.DependentStat.ToString(), mod.ModifierType, mod.TargetModifyTypes, mod.MinMaxValues, mod.ModifyValue.x, mod.ModifyValue.y, mod.ModifierScalingSource, mod.ModifierScalerType.ToString());
            AddTagDescription(mod.ModifierType, mod.TargetStat.ToString(), mod.SelfTags, mod.TargetTags);
            return _sb.ToString();
        }

        public static string GetStatModifierDescription(string targetStat, string dependentStat, ModifierType modifyType, CopyableModifyType targetModifyTypes, MinMaxValues minMaxValues, float minValue, float maxValue,
            DynamicSource scalingSources, string modifierScalingType, IEnumerable<ModifierTagSO> selfTags, IEnumerable<ModifierTagSO> targetTags)
        {
            AddCoreDescription(targetStat, dependentStat, modifyType, targetModifyTypes, minMaxValues, minValue, maxValue, scalingSources, modifierScalingType);
            AddTagDescription(modifyType, targetStat, selfTags, targetTags);
            return _sb.ToString();
        }

        private static void AddCoreDescription(string targetStat, string dependentStat, ModifierType modifyType, CopyableModifyType targetModifyTypes, MinMaxValues minMaxValues, float minValue, float maxValue,
            DynamicSource scalingSources, string modifierScalingType)
        {
            _sb.Clear();
            string modValueString;
            if ((modifyType == ModifierType.ConvertedTo) || (modifyType == ModifierType.AddedAs) ||
                (modifyType == ModifierType.ModifiersAlsoApplyToStat) || (modifyType == ModifierType.ConvertSelfTags) || (modifyType == ModifierType.ConvertTargetTags))
            {
                if (minMaxValues == MinMaxValues.Different)
                {
                    modValueString = (minValue * 100f).ToString("0.##") + "% - " + (maxValue * 100f).ToString("0.##") + "%";
                }
                else
                {
                    modValueString = (minValue * 100f).ToString("0.##") + "%";
                }
            }
            else if (modifyType == ModifierType.SumMultiply || modifyType == ModifierType.FinalSumMultiply)
            {
                if (minMaxValues == MinMaxValues.Different)
                {
                    modValueString = $"{(minValue > 0 ? "+" : "")}{minValue} - {(maxValue > 0 ? "+" : "")}{maxValue}";
                }
                else
                {
                    modValueString = $"{(minValue > 0 ? "+" : "")}{minValue}";
                }
                if (minMaxValues == MinMaxValues.Different)
                {
                    modValueString = $"{(minValue > 0 ? "+" : "")}{(minValue * 100f).ToString("0.##")}% - {(minValue > 0 ? "+" : "")}{(maxValue * 100f).ToString("0.##")}%";
                }
                else
                {
                    modValueString = $"{(minValue > 0 ? "+" : "")}{(minValue * 100f).ToString("0.##")}%";
                }
            }
            else
            {
                if (minMaxValues == MinMaxValues.Different)
                {
                    modValueString = $"{minValue} - {maxValue}";
                }
                else
                {
                    modValueString = $"{minValue}";
                }
            }

            if ((scalingSources & DynamicSource.Self) != 0 && (scalingSources & DynamicSource.Target) != 0)
            {
                _sb.Append("(");
                _sb.Append(modifierScalingType);
                _sb.Append(" of self and target * ");
                _sb.Append(modValueString);
                _sb.Append(")");
            }
            else if ((scalingSources & DynamicSource.Self) != 0)
            {
                _sb.Append("(");
                _sb.Append(modifierScalingType);
                _sb.Append(" of self * ");
                _sb.Append(modValueString);
                _sb.Append(")");
            }
            else if ((scalingSources & DynamicSource.Target) != 0)
            {
                _sb.Append("(");
                _sb.Append(modifierScalingType);
                _sb.Append(" of target * ");
                _sb.Append(modValueString);
                _sb.Append(")");
            }
            else
            {
                _sb.Append(modValueString);
            }

            switch (modifyType)
            {
                case ModifierType.Add:
                    _sb.Append(" added ");
                    break;
                case ModifierType.SumMultiply:
                    _sb.Append(" ");
                    break;
                case ModifierType.ProductMultiply:
                    _sb.Append(" * ");
                    break;
                case ModifierType.AddedAs:
                    _sb.Append(" of ");
                    _sb.Append(Regex.Replace(dependentStat, "(\\B[A-Z0-9])", " $1"));
                    _sb.Append(" added as ");
                    break;
                case ModifierType.ConvertedTo:
                    _sb.Append(" of ");
                    _sb.Append(Regex.Replace(dependentStat, "(\\B[A-Z0-9])", " $1"));
                    _sb.Append(" converted to ");
                    break;
                case ModifierType.ModifiersAlsoApplyToStat:
                    _sb.Append(" of ");
                    _sb.Append(Regex.Replace(dependentStat, "(\\B[A-Z0-9])", " $1"));
                    _sb.Append(" ");
                    break;
                case ModifierType.ConvertSelfTags:
                    _sb.Append(" of ");
                    break;
                case ModifierType.ConvertTargetTags:
                    _sb.Append(" of ");
                    break;
                case ModifierType.FinalAdd:
                    _sb.Append(" added final ");
                    break;
                case ModifierType.FinalSumMultiply:
                    _sb.Append(" final ");
                    break;
                case ModifierType.FinalProductMultiply:
                    _sb.Append("x final ");
                    break;
            }


            if (modifyType == ModifierType.ConvertSelfTags || modifyType == ModifierType.ConvertTargetTags || modifyType == ModifierType.ModifiersAlsoApplyToStat)
            {
                var hasModifyTypeTarget = false;
                if ((targetModifyTypes & CopyableModifyType.BaseAdd) != 0)
                {
                    _sb.Append("additions");
                    hasModifyTypeTarget = true;
                }
                if ((targetModifyTypes & CopyableModifyType.SumMultiply) != 0)
                {
                    if (hasModifyTypeTarget) { _sb.Append(", "); }
                    _sb.Append("sum multiplies");
                    hasModifyTypeTarget = true;
                }
                if ((targetModifyTypes & CopyableModifyType.ProductMultiply) != 0)
                {
                    if (hasModifyTypeTarget) { _sb.Append(", "); }
                    _sb.Append("product multiplies");
                    hasModifyTypeTarget = true;
                }
            }
            if (modifyType == ModifierType.ModifiersAlsoApplyToStat)
            {
                _sb.Append(" also apply to ");
            }
        }

        private static void AddTagDescription(ModifierType modifyType, string targetStat, IEnumerable<ModifierTagSO> selfTags, IEnumerable<ModifierTagSO> targetTags)
        {

            if (modifyType == ModifierType.ConvertSelfTags)
            {
                _sb.Append(" to ");
                foreach (var st in selfTags)
                {
                    _sb.Append($"{st.StringValue} ");
                }
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));

                _sb.Append(" also apply to ");
                foreach (var tt in targetTags)
                {
                    _sb.Append($"{tt.StringValue} ");
                }
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
            }
            else if (modifyType == ModifierType.ConvertTargetTags)
            {
                _sb.Append(" to ");
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
                _sb.Append(" against ");
                foreach (var st in selfTags)
                {
                    _sb.Append($"{st.StringValue} ");
                }

                _sb.Append(" also apply to ");
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
                _sb.Append(" against ");
                foreach (var tt in targetTags)
                {
                    _sb.Append($"{tt.StringValue} ");
                }
            }
            else
            {
                foreach (var st in selfTags)
                {
                    _sb.Append($"{st.StringValue} ");
                }

                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));

                if (targetTags.Count() > 0)
                {
                    _sb.Append(" against ");
                    foreach (var tt in targetTags)
                    {
                        _sb.Append($"{tt.StringValue}");
                    }
                }
            }
        }

        private static void AddTagDescription(ModifierType modifyType, string targetStat, ModifierTagLookup selfTags, ModifierTagLookup targetTags)
        {

            if (modifyType == ModifierType.ConvertSelfTags)
            {
                _sb.Append(" to ");
                Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => selfTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));

                _sb.Append(" also apply to ");
                Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();
                var settags = Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).ToArray();
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
            }
            else if (modifyType == ModifierType.ConvertTargetTags)
            {
                _sb.Append(" to ");
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
                _sb.Append(" against ");
                Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => selfTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();


                _sb.Append(" also apply to ");
                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));
                _sb.Append(" against ");
                Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();

            }
            else
            {
                Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => selfTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();

                _sb.Append(Regex.Replace(targetStat, "(\\B[A-Z0-9])", " $1"));

                if (targetTags.HasAnyTagsSet())
                {
                    _sb.Append(" against ");
                    Enumerable.Range(0, DeepStatsConstants.NumModifierTags).Where(ind => targetTags.IsTagSet((ModifierTag)ind)).Select(ind => _sb.Append($"{(ModifierTag)ind} ")).ToList();
                }
            }
        }
    }
}