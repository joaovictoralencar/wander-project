using System.IO;
using LukeyB.DeepStats.GameObjects;
using UnityEditor;
using UnityEngine;
using LukeyB.DeepStats.ScriptableObjects;

namespace Assets.DeepStats.EditorScripts
{
    public class DeepStatsManagerEditor : MonoBehaviour
    {
        [MenuItem("Tools/DeepStats/Initialise Scene")]
        static void CreateDeepStatsManager()
        {
            var instance = new GameObject("DeepStatsManager");

            Debug.unityLogger.logEnabled = false;   // disable logs so we don't show the onValidate error (it will get fixed below)
            var manager = instance.AddComponent<DeepStatsManager>();
            Debug.unityLogger.logEnabled = true;

            // find a configuration and set it, otherwise create a new one
            var existingConfigs = AssetDatabase.FindAssets("t:DeepStatsConfigurationSO");
            for (var i = 0; i < existingConfigs.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<DeepStatsConfigurationSO>(AssetDatabase.GUIDToAssetPath(existingConfigs[i]));

                // if there is another option, dont set the default config that comes with DeepStats demos
                // use the user created option
                if (asset.name == "DemoConfiguration" && i < existingConfigs.Length - 1)
                {
                    continue;
                }

                manager.SetConfigurationEditorOnly(asset);
                return;
            }

            // No configs created yet, build one for the user
            DeepStatsConfigurationSO newConfig = ScriptableObject.CreateInstance<DeepStatsConfigurationSO>();
            var assetPath = "Assets/DeepStatsConfiguration.asset";
            AssetDatabase.CreateAsset(newConfig, assetPath);
            AssetDatabase.SaveAssets();

            manager.SetConfigurationEditorOnly(newConfig);
        }
    }
}