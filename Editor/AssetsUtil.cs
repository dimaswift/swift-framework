using System;
using UnityEditor;

namespace Swift.EditorUtils
{
    public class AssetsUtil : AssetPostprocessor
    {
        public static event Action OnAssetsPostProcessed = () => { };
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            OnAssetsPostProcessed();
        }

        public static void TriggerPostprocessEvent()
        {
            OnAssetsPostProcessed();
        }
    }
}