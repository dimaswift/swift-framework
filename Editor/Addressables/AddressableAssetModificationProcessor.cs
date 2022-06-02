using System;
using System.IO;
using UnityEditor;
using UnityEngine;

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
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string assetName, string destinationPath)
        {
            AddrHelper.ReloadOnPostProcess = true;
            return AssetMoveResult.DidNotMove;
        }
    }
#endif
}