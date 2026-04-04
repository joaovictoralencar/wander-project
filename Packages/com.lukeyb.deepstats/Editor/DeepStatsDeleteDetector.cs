using Assets.DeepStats;
using LukeyB.DeepStats.ScriptableObjects;
using LukeyB.DeepStats.User;
using UnityEditor;
using UnityEngine;

namespace Packages.com.lukeb.deepstats.Editor
{
    public class DeepStatsDeleteDetector : UnityEditor.AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (assetType == typeof(StatTypeSO) ||
                assetType == typeof(ModifierScalerSO) ||
                assetType == typeof(ModifierTagSO))
            {
                Debug.LogError($"DeepStats Configuration is out of sync and needs to be regenerated. A config asset has been deleted.");
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}