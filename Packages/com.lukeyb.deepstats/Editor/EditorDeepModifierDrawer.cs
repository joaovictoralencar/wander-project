using UnityEditor;
using UnityEngine;
using System.Linq;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Enums;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.ScriptableObjects;

namespace Assets.DeepStats.EditorScripts
{
    [CustomPropertyDrawer(typeof(EditorDeepModifier))]
    public class EditorDeepModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Extract properties into variables
            SerializedProperty modifyTypeProperty = property.FindPropertyRelative("ModifierType");
            SerializedProperty MinMaxValuesProperty = property.FindPropertyRelative("MinMaxValues");
            SerializedProperty modifierMinValueProperty = property.FindPropertyRelative("ModifyValue").FindPropertyRelative("x");
            SerializedProperty modifierMaxValueProperty = property.FindPropertyRelative("ModifyValue").FindPropertyRelative("y");
            SerializedProperty targetStatProperty = property.FindPropertyRelative("TargetStat");
            SerializedProperty dependentStatProperty = property.FindPropertyRelative("DependentStat");
            SerializedProperty targetModifyProperty = property.FindPropertyRelative("TargetModifyTypes");

            SerializedProperty modifierScalingSourcesProperty = property.FindPropertyRelative("ModifierScalingSource");
            SerializedProperty modifierScalerTypeProperty = property.FindPropertyRelative("ModifierScalerType");
            SerializedProperty scalingClampMinProperty = property.FindPropertyRelative("ScalingClamp").FindPropertyRelative("x");
            SerializedProperty scalingClampMaxProperty = property.FindPropertyRelative("ScalingClamp").FindPropertyRelative("y");

            SerializedProperty selfTagsProperty = property.FindPropertyRelative("SelfTags");
            SerializedProperty targetTagsProperty = property.FindPropertyRelative("TargetTags");

            SerializedProperty validationErrorProperty = property.FindPropertyRelative("ValidationError");

            var modifyType = (ModifierType)modifyTypeProperty.intValue;
            var minMaxValues = (MinMaxValues)MinMaxValuesProperty.intValue;
            var modifierMinValue = modifierMinValueProperty.floatValue;
            var modifierMaxValue = modifierMaxValueProperty.floatValue;
            var targetStat = targetStatProperty.objectReferenceValue?.name;
            var dependentStat = dependentStatProperty.objectReferenceValue?.name;
            var targetModify = (CopyableModifyType)targetModifyProperty.enumValueFlag;

            var modifierScalingSources = (DynamicSource)modifierScalingSourcesProperty.enumValueFlag;
            var modifierScalerTypeValue = modifierScalerTypeProperty.objectReferenceValue?.name;
            var scalingClampMin = scalingClampMinProperty.floatValue;
            var scalingClampMax = scalingClampMaxProperty.floatValue;

            var selfTags = Enumerable.Range(0, selfTagsProperty.arraySize).Select(i => selfTagsProperty.GetArrayElementAtIndex(i).objectReferenceValue as ModifierTagSO);
            var targetTags = Enumerable.Range(0, targetTagsProperty.arraySize).Select(i => targetTagsProperty.GetArrayElementAtIndex(i).objectReferenceValue as ModifierTagSO);

            validationErrorProperty.stringValue = "";
            if (modifyType == ModifierType.ModifiersAlsoApplyToStat || modifyType == ModifierType.ConvertSelfTags || modifyType == ModifierType.ConvertTargetTags)
            {
                if (targetModify == CopyableModifyType.None)
                {
                    validationErrorProperty.stringValue = "Target Modify Types must not be None";
                }
            }

            if (modifyType == ModifierType.ConvertSelfTags || modifyType == ModifierType.ConvertTargetTags)
            {
                if (selfTagsProperty.arraySize == 0)
                {
                    validationErrorProperty.stringValue = "Cannot have a matching tag modifier with no required tags";
                }
                else if (targetTagsProperty.arraySize == 0)
                {
                    validationErrorProperty.stringValue = "Cannot have a matching tag modifier with no new tags";
                }
            }

            if (targetStatProperty.objectReferenceValue == null)
            {
                validationErrorProperty.stringValue = "A target stat is required";
            }

            else if ((modifyType == ModifierType.ConvertedTo) || (modifyType == ModifierType.AddedAs) ||
                (modifyType == ModifierType.ModifiersAlsoApplyToStat))
            {
                if (dependentStatProperty.objectReferenceValue == null)
                {
                    validationErrorProperty.stringValue = "A Dependent Stat is required if Modify Type depends on a second Stat Type";
                }
                else
                {
                    var dependentEnum = (int)((ScriptableEnum<StatType>)dependentStatProperty.objectReferenceValue).EnumValue;
                    var targetEnum = (int)((ScriptableEnum<StatType>)targetStatProperty.objectReferenceValue).EnumValue;
                    if (dependentEnum >= targetEnum)
                    {
                        validationErrorProperty.stringValue = "Dependent Stat cannot be lower priority than Target stat. Adjust your Stat priority in your Stat Configuration";
                    }
                }
            }

            if (selfTags.Any(st => st == null))
            {
                validationErrorProperty.stringValue = "Cannot have an empty self tag";
            }
            if (targetTags.Any(st => st == null))
            {
                validationErrorProperty.stringValue = "Cannot have an empty target tag";
            }

            if (modifierScalingSources != DynamicSource.None)
            {
                if (modifierScalerTypeProperty.objectReferenceValue == null)
                {
                    validationErrorProperty.stringValue = "A Modifier Scaler Type must be set if Scaling Source is not None";
                }
            }

            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            string foldOutLabel;
            if (validationErrorProperty.stringValue != "")
            {
                foldOutLabel = $"Error: {validationErrorProperty.stringValue}";
                foldoutStyle.normal.textColor = Color.red; // Change color as needed
                foldoutStyle.onNormal.textColor = Color.red;
                foldoutStyle.onFocused.textColor = Color.red;
            }
            else
            {
                foldOutLabel = DeepStatsUtils.GetStatModifierDescription(
                    targetStat, dependentStat,
                    modifyType, targetModify, minMaxValues, modifierMinValue, modifierMaxValue,
                    modifierScalingSources, modifierScalerTypeValue, selfTags, targetTags);
            }

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent(foldOutLabel), foldoutStyle);

            if (property.isExpanded)
            {
                EditorGUILayout.PropertyField(modifyTypeProperty, new GUIContent("Modifier Type"));

                if (modifyType == ModifierType.ModifiersAlsoApplyToStat || modifyType == ModifierType.ConvertSelfTags || modifyType == ModifierType.ConvertTargetTags)
                {
                    EditorGUILayout.PropertyField(targetModifyProperty, new GUIContent("Target Modify Types"));
                }

                var mustBeSame = modifyType == ModifierType.FinalAdd || modifyType == ModifierType.FinalSumMultiply || modifyType == ModifierType.FinalProductMultiply;

                if (mustBeSame)
                {
                    minMaxValues = MinMaxValues.Same;
                    MinMaxValuesProperty.intValue = (int)minMaxValues;
                }
                else
                {
                    EditorGUILayout.PropertyField(MinMaxValuesProperty, new GUIContent("Set Min / Max Values"));
                }

                if (minMaxValues == MinMaxValues.Different)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(modifierMinValueProperty, new GUIContent("Min Value"), GUILayout.MinWidth(100));
                    EditorGUILayout.PropertyField(modifierMaxValueProperty, new GUIContent("Max Value"), GUILayout.MinWidth(100));
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.PropertyField(modifierMinValueProperty, new GUIContent("Modify Value"));
                    modifierMaxValueProperty.floatValue = modifierMinValueProperty.floatValue;
                }

                EditorGUILayout.Space();

                if (modifyType == ModifierType.AddedAs ||
                    modifyType == ModifierType.ConvertedTo ||
                    modifyType == ModifierType.ModifiersAlsoApplyToStat)
                {
                    EditorGUILayout.PropertyField(dependentStatProperty, new GUIContent("Dependent Stat"));
                }

                EditorGUILayout.PropertyField(targetStatProperty, new GUIContent("Target Stat"));

                EditorGUILayout.Space();

                // noooo idea why, but editor wont let you pick an option if you access the flag after drawing the property
                EditorGUILayout.PropertyField(modifierScalingSourcesProperty);
                if (modifierScalingSources != 0)
                {
                    EditorGUILayout.PropertyField(modifierScalerTypeProperty, new GUIContent("Modifier Scaler"));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Clamp Scaler Range"));
                    EditorGUILayout.PropertyField(scalingClampMinProperty, GUIContent.none, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.15f));
                    EditorGUILayout.PropertyField(scalingClampMaxProperty, GUIContent.none, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.15f));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();

                var tagWidth = GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.5f);

                if (modifyType == ModifierType.ConvertSelfTags || modifyType == ModifierType.ConvertTargetTags)
                {
                    EditorGUILayout.PropertyField(selfTagsProperty, new GUIContent("Required Tags"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(targetTagsProperty, new GUIContent("New Tags"));
                    EditorGUILayout.Space();
                }
                else
                {
                    EditorGUILayout.PropertyField(selfTagsProperty, new GUIContent("Self Tags"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(targetTagsProperty, new GUIContent("Target Tags"));
                    EditorGUILayout.Space();
                }
            }
        }
    }
}