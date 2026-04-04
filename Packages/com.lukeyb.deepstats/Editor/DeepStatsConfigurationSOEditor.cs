
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Assets.DeepStats.EditorScripts
{
    [CustomEditor(typeof(DeepStatsConfigurationSO))]
    public class DeepStatsConfigurationSOEditor : Editor
    {
        private static readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private static readonly string[] StatNames = Enum.GetNames(typeof(StatType));
        private static readonly string[] ScalerNames = Enum.GetNames(typeof(ModifierScaler));
        private static readonly string[] TagNames = Enum.GetNames(typeof(ModifierTag));

        private GUIStyle _selectedStyle;
        private GUIStyle SelectedStyle { get { if (_selectedStyle == null || _selectedStyle.normal.background == null) CreateStyle(); return _selectedStyle; } }

        private UnityEngine.Object selectedObject = null; // Initialize as -1 to indicate no selection

        private void CreateStyle()
        {
            Texture2D texture = new Texture2D(1, 1);
            var c = Color.cyan;
            c.a = 0.3f;
            texture.SetPixel(0, 0, c); 
            texture.Apply();

            // Create GUIStyle for selected rows
            _selectedStyle = new GUIStyle(EditorStyles.helpBox);
            _selectedStyle.normal.background = texture; 
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DeepStatsConfigurationSO script = (DeepStatsConfigurationSO)target;

            //if (script.StatTypes == null)
            //{
            //    script.StatTypes = new StatTypeSO[0];
            //}
            //if (script.ModifierScalers == null)
            //{
            //    script.ModifierScalers = new ModifierScalerSO[0];
            //}
            //if (script.ModifierTags == null)
            //{
            //    script.ModifierTags = new ModifierTagSO[0];
            //}

            EditorGUILayout.Space();

            var serializedStatObjects = serializedObject.FindProperty("StatTypes");
            var modifierScalersObjects = serializedObject.FindProperty("ModifierScalers");
            var modifierTagsObjects = serializedObject.FindProperty("ModifierTags");

            var statTypeResult = SerializeObjectsToArrayAndValidate(typeof(StatTypeSO), typeof(StatType), serializedStatObjects, script.StatTypes, null, StatNames);
            var scalerResult = SerializeObjectsToArrayAndValidate(typeof(ModifierScalerSO), typeof(ModifierScaler), modifierScalersObjects, script.ModifierScalers, null, ScalerNames);
            var tagResult = SerializeObjectsToArrayAndValidate(typeof(ModifierTagSO), typeof(ModifierTag), modifierTagsObjects, script.ModifierTags, null, TagNames);
            serializedObject.ApplyModifiedProperties();

            var isError = statTypeResult.nameError || scalerResult.nameError || tagResult.nameError;
            if (isError)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.HelpBox($"Some names are not convertible to C#, check the list below and update the names or let DeepStats try and fix them for you", MessageType.Error);

                if (GUILayout.Button("Make names compatible with C#"))
                {
                    CleanUpScriptableObjectNames(typeof(StatTypeSO));
                    CleanUpScriptableObjectNames(typeof(ModifierScalerSO));
                    CleanUpScriptableObjectNames(typeof(ModifierTagSO));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (!isError && (statTypeResult.outOfSync || scalerResult.outOfSync || tagResult.outOfSync))
            {
                EditorGUILayout.HelpBox($"Generated code is out of sync, regenerate scripts when ready", MessageType.Error);

                if (GUILayout.Button("Generate C# enums and constants"))
                {
                    GenerateEnumsAndConstants(script.StatTypes, script.ModifierScalers, script.ModifierTags);
                }
            }
            else if (!isError)
            {
                EditorGUILayout.HelpBox($"Generated code is in sync", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stat conversions can only flow down. Use the arrows to re-order your stats");
            DrawSerializedObjectInspector(serializedStatObjects, "Stat Types", true);
            EditorGUILayout.Space();
            DrawSerializedObjectInspector(modifierScalersObjects, "Modifier Scalers", false);
            EditorGUILayout.Space();
            DrawSerializedObjectInspector(modifierTagsObjects, "Modifier Tags", false);
            serializedObject.ApplyModifiedProperties();
        }

        private (bool nameError, bool outOfSync) SerializeObjectsToArrayAndValidate(Type objectType, Type enumType, SerializedProperty serializedArray, ScriptableObject[] currentItems, string[] reservedSlots, string[] enumNames)
        {
            // not sure why unity doesnt always initialise the public property but this is a prevention for that
            if (currentItems == null)
            {
                currentItems = new ScriptableObject[0]; 
            }

            bool errorInNames = false;
            bool codeOutOfSync = false;

            var numReservedSlots = 0;

            if (reservedSlots != null)
            {
                numReservedSlots = reservedSlots.Length;

                for (int i = 0; i < numReservedSlots; i++)
                {
                    if (i >= enumNames.Length || enumNames[i] != reservedSlots[i])
                    {
                        codeOutOfSync = true;  // enum does not include reserved elements
                    }
                }
            }

            var guids = AssetDatabase.FindAssets("t:" + objectType.Name);
            _assets.Clear();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var scriptableObjects = AssetDatabase.LoadAssetAtPath<ScriptableEnum>(path);
                _assets.Add(scriptableObjects);

                if (scriptableObjects.StringValue != scriptableObjects.name)
                {
                    codeOutOfSync = true;
                }
            }

            // check for any added types
            for (int i = 0; i < _assets.Count; i++)
            {
                if (!currentItems.Contains(_assets[i]))
                {
                    serializedArray.arraySize++;
                    var objectElement = serializedArray.GetArrayElementAtIndex(serializedArray.arraySize-1);
                    objectElement.objectReferenceValue = _assets[i];
                }

                var error = IsValidEnumMemberName(_assets[i].name);
                if (error != null)
                {
                    errorInNames = true;
                }

                if (!Enum.TryParse(enumType, _assets[i].name, out var parsed))
                {
                    codeOutOfSync = true;
                }
            }

            // check for any removed types
            // iterate backwards so we dont mess with the order when removing
            for (int i = currentItems.Length - 1; i >= 0; i--)
            {
                if (currentItems[i] == null)
                {
                    serializedArray.DeleteArrayElementAtIndex(i);
                }
            }

            if (serializedArray.arraySize + numReservedSlots != enumNames.Length)
            {
                codeOutOfSync = true;  // if enum is different length, dont bother checking individual elements
            }
            else
            {
                for (var i = 0; i < serializedArray.arraySize; i++)
                {
                    if (i < numReservedSlots)
                    {
                        continue;
                    }

                    var configElement = serializedArray.GetArrayElementAtIndex(i);
                    if (configElement.objectReferenceValue.name != enumNames[i + numReservedSlots])
                    {
                        codeOutOfSync = true;
                    }
                }
            }
            return (errorInNames, codeOutOfSync);
        }

        private void DrawSerializedObjectInspector(SerializedProperty objectArray, string header, bool includeOrdering)
        {
            EditorGUILayout.LabelField(header);
            for (int i = 0; i < objectArray.arraySize; i++)
            {
                SerializedProperty serializedObject = objectArray.GetArrayElementAtIndex(i);
                var scriptableObject = serializedObject.objectReferenceValue;

                // Check if this row is selected
                bool isSelected = scriptableObject == selectedObject;
                // Determine GUI style based on selection
                GUIStyle style = isSelected ? SelectedStyle : EditorStyles.helpBox;

                EditorGUILayout.BeginVertical(style);
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject, GUIContent.none);
                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                if (includeOrdering)
                {
                    if (i != 0 && GUILayout.Button("\u25B2", GUILayout.Width(20))) // Up arrow
                    {
                        objectArray.MoveArrayElement(i, i - 1);
                        selectedObject = scriptableObject;
                    }

                    if (i != objectArray.arraySize - 1 && GUILayout.Button("\u25BC", GUILayout.Width(20))) // Down arrow
                    {
                        objectArray.MoveArrayElement(i, i + 1);
                        selectedObject = scriptableObject;
                    }
                }

                EditorGUILayout.EndHorizontal();

                var error = IsValidEnumMemberName(scriptableObject.name);
                if (error != null)
                {
                    EditorGUILayout.HelpBox($"{scriptableObject.name} is not a valid C# enum name:\n{error}", MessageType.Error);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void CleanUpScriptableObjectNames(Type type)
        {
            var guids = AssetDatabase.FindAssets("t:" + type.Name);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                var cleanedName = CleanUpEnumMemberName(asset.name);
                AssetDatabase.RenameAsset(path, cleanedName);
            }
        }

        private static void GenerateEnumFromStrings(string scriptName, string path, IEnumerable<string> enumNames)
        {
            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace LukeyB.DeepStats.User\r\n{");
                streamWriter.WriteLine("// AUTO GENERATED");
                streamWriter.WriteLine("// Use a StatConfiguration scriptable object asset to modify this enum");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\tpublic enum " + scriptName);
                streamWriter.WriteLine("\t{");

                foreach (var enumName in enumNames) 
                {
                    streamWriter.WriteLine("\t\t" + enumName + ",");
                }

                streamWriter.WriteLine("\t}\r\n}");
            }
        }

        public static void GenerateEnumsAndConstants(StatTypeSO[] statTypes, ModifierScalerSO[] scalers, ModifierTagSO[] tags)
        {
            // Stat Types
            GenerateEnumFromStrings("StatType", "Packages/com.lukeyb.deepstats/Scripts/User/StatType.cs", statTypes.Select(so => so.name));
            foreach (var s in statTypes)
            {
                s.StringValue = s.name;
                EditorUtility.SetDirty(s);
            }

            // scalers
            GenerateEnumFromStrings("ModifierScaler", "Packages/com.lukeyb.deepstats/Scripts/User/ModifierScaler.cs", scalers.Select(so => so.name));
            foreach (var s in scalers)
            {
                s.StringValue = s.name;
                EditorUtility.SetDirty(s);
            }

            // tags
            GenerateEnumFromStrings("ModifierTag", "Packages/com.lukeyb.deepstats/Scripts/User/ModifierTag.cs", tags.Select(so => so.name));
            foreach (var s in tags)
            {
                s.StringValue = s.name;
                EditorUtility.SetDirty(s);
            }

            GenerateConstants("Packages/com.lukeyb.deepstats/Scripts/User/DeepStatsConstants.cs", statTypes.Length, scalers.Length, tags.Length);
            GenerateFlagStorage("Packages/com.lukeyb.deepstats/Scripts/User/ModifierTagLookup.Storage.cs", tags.Length, "LukeyB.DeepStats.User", "ModifierTagLookup");
            GenerateFlagStorage("Packages/com.lukeyb.deepstats/Scripts/Core/StatTypeGroup.Storage.cs", statTypes.Length, "LukeyB.DeepStats.Core", "StatTypeGroup");
            AssetDatabase.Refresh();
        }

        public static void GenerateConstants(string path, int statTypes, int modifierScalers, int modifierTags)
        {
            string code = @"

namespace LukeyB.DeepStats.User
{
    public static class DeepStatsConstants
    {
        // Length of StatTypes list
        public const int NumStatTypes = " + statTypes + @";

        // Length of ModifierScalers list
        public const int NumModifierScalers = " + modifierScalers + @";

        // Length of ModifierTags list
        public const int NumModifierTags = " + modifierTags + @";

        // Number of ways a stat can be modified, used to build arrays to track modifications
        public const int NumModifyTypes = 4;   // four sources to a stat: add, sumMultiply, productMultiply, conversion from another stat

        // Offset of final modifier types in the enum, used correct access to index in final modification array
        public const int FinalModifierTypeOffset = 1000;
    }
}";

            File.WriteAllText(path, code);
        }

        public static void GenerateFlagStorage(string path, int numTags, string namespaceName, string className)
        {
            // this should make f0, f1, f2 etc. for storing as many tags as needed in bits
            var numFlagFields = (int)((float)(numTags-1) / (sizeof(int) * 8)) + 1;
            var storageFields = String.Join(", ", Enumerable.Range(0, numFlagFields).Select(flagsInd => $"f{flagsInd}"));

            string code = @"
using System.Runtime.InteropServices;

namespace " + namespaceName + @"
{
    // AUTO GENERATED
    // Use a StatConfiguration scriptable object asset to modify this

    [StructLayout(LayoutKind.Sequential)]   // we use sequential pointers to access each set of tags, make sure its in memory that way
    public partial struct " + className + @"
    {
        private int " + storageFields + @";
        private const int NumParts = " + numFlagFields + @";
        private const int NumFlags = " + numTags + @";
    }
}";

            File.WriteAllText(path, code);
        }

        public static string IsValidEnumMemberName(string name)
        {
            // Check if name is not empty
            if (string.IsNullOrWhiteSpace(name))
                return "Name must not be empty";

            // Check if the first character is valid
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return "Name must start with a letter or underscore";

            // Check the rest of the characters
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return "Name can only consist of letters, numbers and underscores. Spaces are not allowed either.";
            }

            // Check if the name is not a C# keyword
            if (IsCSharpKeyword(name))
                return name + " is a reserved C# keyword. You cannot use this as a name";

            return null;
        }

        // Method to check if a name is a C# keyword
        public static bool IsCSharpKeyword(string name)
        {
            // List of C# keywords
            string[] keywords = new string[]
            {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum", "event", "explicit", "extern", "false",
            "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
            "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
            "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
            "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile",
            "while"
            };

            return keywords.Contains(name);
        }

        // Method to clean up an invalid enum member name
        public static string CleanUpEnumMemberName(string name)
        {
            // Remove leading and trailing whitespaces
            name = name.Trim();

            // Replace invalid characters with underscores
            StringBuilder cleanedName = new StringBuilder(name.Length);
            bool capitalizeNext = false;

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (char.IsWhiteSpace(c))
                {
                    capitalizeNext = true;
                }
                else
                {
                    if (capitalizeNext)
                    {
                        cleanedName.Append(char.ToUpper(c));
                        capitalizeNext = false;
                    }
                    else
                    {
                        cleanedName.Append(c);
                    }
                }
            }

            // Ensure the cleaned name starts with a valid character
            if (!char.IsLetter(cleanedName[0]) && cleanedName[0] != '_')
            {
                cleanedName.Insert(0, '_');
            }

            // Check if the cleaned name is still a C# keyword
            string cleanedNameStr = cleanedName.ToString();
            if (IsCSharpKeyword(cleanedNameStr))
            {
                cleanedNameStr += "_"; // Append underscore to avoid keyword conflict
            }

            return cleanedNameStr;
        }

    }
}

