using System.IO;
using UnityEditor;

namespace SwiftFramework.Core.Editor
{
    #if USE_ADDRESSABLES
    public class AddressableAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        static void OnWillCreateAsset(string assetName)
        {
            AddrHelper.ReloadOnPostProcess = true;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetName, RemoveAssetOptions options)
        {
            AddrHelper.ReloadOnPostProcess = true;
            File.Delete(assetName);
            File.Delete(assetName + ".meta");
            AssetDatabase.Refresh();
            return AssetDeleteResult.DidDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string assetName, string destinationPath)
        {
            AddrHelper.ReloadOnPostProcess = true;
            File.Move(assetName, destinationPath);
            File.Move(assetName + ".meta", destinationPath + ".meta");
            AssetDatabase.Refresh();
            return AssetMoveResult.DidMove;
        }
    }
#endif
}