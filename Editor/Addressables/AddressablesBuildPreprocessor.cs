using SwiftFramework.EditorUtils;
using UnityEditor;
#if USE_ADDRESSABLES
using SwiftFramework.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SwiftFramework.Core.Editor
{
    #if USE_ADDRESSABLES
    
    internal static class AddressablesBuildPreprocessor
    {
        private static void PreExport()
        {
            AddressableAssetSettings.CleanPlayerContent(
                AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent();
        }
 
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildHook.OnBeforeBuild += BuildPlayerHandler;
        }
 
        private static void BuildPlayerHandler()
        {
                       
#if  UNITY_ANDROID
            BootConfig bootConfig = Util.FindScriptableObject<BootConfig>();
            bootConfig.buildNumber = PlayerSettings.Android.bundleVersionCode;
            EditorUtility.SetDirty(bootConfig);
#endif
            if (EditorUtility.DisplayDialog("Build with Addressables",
                "Do you want to build a clean addressables before export?",
                "Build with Addressables", "Skip"))
            {
                PreExport();
            }
        }
    }
    
    #endif
}
