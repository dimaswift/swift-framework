using System;
using UnityEditor;

namespace SwiftFramework.Core.Editor
{
    public class AssetsProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains($"{ResourcesAssetHelper.RootFolder}/{Folders.Sprites}/"))
            {
                TextureImporter importer = (TextureImporter) assetImporter;
                importer.textureType = TextureImporterType.Sprite;
            }
        }
    }
}