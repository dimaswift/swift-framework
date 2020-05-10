using SwiftFramework.EditorUtils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    internal static class AssetHelper
    {
        public static Action OnReload = () => { };

        internal static IEnumerable<LegacyAssetEntry> GetScenes()
        {
            foreach (var item in Util.GetAssets<SceneAsset>())
            {
                yield return new LegacyAssetEntry()
                {
                    address = item.name,
                    asset = item,
                    AssetPath = AssetDatabase.GetAssetPath(item)
                };
            }
        }

        internal static IEnumerable<LegacyAssetEntry> GetAssets(Type type)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                foreach (var asset in Util.GetAssets(typeof(GameObject), "", "Assets/Resources"))
                {
                    var go = asset as GameObject;
                    if (go && go.GetComponent(type))
                    {
                        yield return LegacyAssetEntry.Create(asset);
                    }
                }
            }
            else
            {
                foreach (var asset in Util.GetAssets(type, "", "Assets/Resources"))
                {
                    yield return LegacyAssetEntry.Create(asset);
                }
            }

         
        }


        public static IEnumerable<LegacyAssetEntry> GetPrefabsWithComponent(Type component)
        {
            foreach (var go in Util.GetAssets<GameObject>())
            {
                if (go != null && go.GetComponent(component) != null)
                {
                    yield return LegacyAssetEntry.Create(go);
                }
            }
        }

        public static IEnumerable<LegacyAssetEntry> GetScriptableObjectsWithInterface(Type @interface)
        {
            foreach (var so in Util.GetAssets<ScriptableObject>())
            {
                if (so != null && @interface.IsAssignableFrom(so.GetType()))
                {
                    yield return LegacyAssetEntry.Create(so);
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            OnReload();
        }

    }
}
