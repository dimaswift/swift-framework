using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class ResourcesAssetEntry
    {
        public string AssetPath;
        public string address;
        public Object asset;

        public static string GetAddress(string path)
        {
            var ext = Path.GetExtension(path);
            return path.Substring(ResourcesAssetHelper.RESOURCES_ROOT_FOLDER.Length + 1, path.Length - (ResourcesAssetHelper.RESOURCES_ROOT_FOLDER.Length + 1) - ext.Length);
        }

        public static ResourcesAssetEntry Create(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);

            return new ResourcesAssetEntry()
            {
                address = GetAddress(path),
                asset = asset,
                AssetPath = path
            };
        }
    }
}