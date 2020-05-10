using SwiftFramework.EditorUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    internal class ResourcesAssetHelper : AssetPostprocessor
    {
        public const string RESOURCES_ROOT_FOLDER = "Assets/Resources";

        public static string RootFolder
        {
            get
            {
#if USE_ADDRESSABLES
                return AddrHelper.ROOT_FOLDER;
#else
                return RESOURCES_ROOT_FOLDER;
#endif
            }
        }

        public static string RootFolderName
        {
            get
            {
#if USE_ADDRESSABLES
                return "Addressables";
#else
                return "Resources";
#endif
            }
        }

        public static Action OnReload = () => { };

        internal static IEnumerable<ResourcesAssetEntry> GetScenes()
        {
            foreach (var item in Util.GetAssets<SceneAsset>("", RESOURCES_ROOT_FOLDER))
            {
                var path = AssetDatabase.GetAssetPath(item);
                yield return new ResourcesAssetEntry()
                {
                    address = ResourcesAssetEntry.GetAddress(AssetDatabase.GetAssetPath(item)),
                    asset = item,
                    AssetPath = path
                };
            }
        }


        internal static IEnumerable<ResourcesAssetEntry> GetAssets(Type type)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                foreach (var asset in Util.GetAssets(typeof(GameObject), "", RESOURCES_ROOT_FOLDER))
                {
                    var go = asset as GameObject;
                    if (go && go.GetComponent(type))
                    {
                        yield return ResourcesAssetEntry.Create(asset);
                    }
                }
            }
            else
            {
                foreach (var asset in Util.GetAssets(type, "", RESOURCES_ROOT_FOLDER))
                {
                    yield return ResourcesAssetEntry.Create(asset);
                }
            }
        }


        public static IEnumerable<ResourcesAssetEntry> GetPrefabsWithComponent(Type component)
        {
            foreach (var go in Util.GetAssets<GameObject>())
            {
                if (go != null && go.GetComponent(component) != null)
                {
                    yield return ResourcesAssetEntry.Create(go);
                }
            }
        }

        public static IEnumerable<ResourcesAssetEntry> GetScriptableObjectsWithInterface(Type @interface)
        {
            foreach (var so in Util.GetAssets<ScriptableObject>())
            {
                if (so != null && @interface.IsAssignableFrom(so.GetType()))
                {
                    yield return ResourcesAssetEntry.Create(so);
                }
            }
        }

#if !USE_ADDRESSABLES
        private static bool reloading;

        [MenuItem("SwiftFramework/Links/Reload")]
        private static void ManualReload()
        {
            Util.PromptMoveAssetsFromFolder("Addressables", "Resources");
            Reload();
        }

        private static void Reload()
        {
            if (reloading)
            {
                return;
            }

            reloading = true;

            foreach (string guid in AssetDatabase.FindAssets("t: Object", new string[] { RESOURCES_ROOT_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Process(path);
            }

            OnReload();

            reloading = false;
        }

        private static void Process(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (asset.TryGetAttribute(out Type singletonType, out AddrSingletonAttribute singleton))
            {
                var scope = path.ToRelativeResourcesPath();
                if (scope != singleton.folder)
                {
                    var assetName = new FileInfo(path).Name;
                    if (scope.FromRelativeResourcesPathToAbsoluteProjectPath() != singleton.folder.FromRelativeResourcesPathToAbsoluteProjectPath())
                    {
                        var oldPath = $"{scope.FromRelativeResourcesPathToAbsoluteProjectPath()}/{assetName}";
                        var newPath = $"{singleton.folder.FromRelativeResourcesPathToAbsoluteProjectPath()}/{assetName}";

                        var dir = new FileInfo(newPath).Directory;

                        if (Directory.Exists(dir.FullName) == false)
                        {
                            Directory.CreateDirectory(dir.FullName);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        }

                        AssetDatabase.MoveAsset(oldPath, newPath);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                        Debug.LogWarning($"Asset with [AddrSingletonAttribute] is moved from {oldPath} to {newPath}"); 
                    }
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                Process(path);
            }

            foreach (var path in movedAssets)
            {
                Process(path);
            }

            foreach (var path in movedFromAssetPaths)
            {
                Process(path);
            }
        }
#endif


    }
}
