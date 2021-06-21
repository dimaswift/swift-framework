﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using Swift.EditorUtils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Swift.Core.Editor
{
    public class ResourcesAssetHelper : AssetPostprocessor
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

        public static event Action OnReload = () => { };

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

        public static T CreateLink<T>(Object asset) where T : Link, new()
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return Link.Create<T>(ToRelativePath(path).Replace(Path.GetExtension(path), ""));
        }

        internal static IEnumerable<ResourcesAssetEntry> GetAssets(Type type)
        {
            Util.EnsureProjectFolderExists(RESOURCES_ROOT_FOLDER);
            
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

#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Generate Drawers")]
#endif
        private static void GenerateDrawers()
        {
            Dictionary<string, CodeCompileUnit> classes = new Dictionary<string, CodeCompileUnit>();

            foreach (var item in Util.GetAllTypesWithAssemblyPath())
            {
                var type = item.type;

                if (type.IsVisible == false)
                {
                    continue;
                }

                if (type.IsGenericType
                    || type == typeof(Link)
                    || type.BaseType == null
                    || type.BaseType.GetGenericArguments().Length == 0
                    || typeof(Link).IsAssignableFrom(type) == false)
                {
                    continue;
                }

                CodeTypeDeclaration drawerClass = new CodeTypeDeclaration($"{type.Name}Drawer");

                drawerClass.BaseTypes.Add(new CodeTypeReference("LinkPropertyDrawer",
                    new[] {new CodeTypeReference(type.BaseType.GetGenericArguments()[0])}));

                drawerClass.CustomAttributes.Add(new CodeAttributeDeclaration(
                    new CodeTypeReference("CustomPropertyDrawer"),
                    new CodeAttributeArgument(new CodeTypeOfExpression(type))));

                if (classes.TryGetValue(item.assemblyLocation, out CodeCompileUnit file) == false)
                {
                    file = new CodeCompileUnit();
                    CodeNamespace namespaces = new CodeNamespace("SwiftFramework.Core.Editor");
                    namespaces.Imports.Add(new CodeNamespaceImport("UnityEditor"));
                    file.Namespaces.Add(namespaces);
                    classes.Add(item.assemblyLocation, file);
                }

                file.Namespaces[0].Types.Add(drawerClass);
            }

            foreach (KeyValuePair<string, CodeCompileUnit> c in classes)
            {
                FileInfo file = new FileInfo(c.Key);
                var dir = Path.Combine(file.Directory.FullName, "Editor");

                if (dir.StartsWith(Application.dataPath.Replace('/', Path.DirectorySeparatorChar)) == false)
                {
                    continue;
                }

                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }

                string filePath = $"{dir}/LinkDrawers.cs";

                ScriptBuilder.SaveClassToDisc(c.Value, filePath, false);
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

        public static string ToRelativePath(string projectPath)
        {
            return projectPath.Substring(RootFolder.Length + 1,
                projectPath.Length - RootFolder.Length - 1);
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
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Reload")]
#endif
        private static void ManualReload()
        {
            Util.PromptMoveAssetsFromFolder("Addressables", "Resources");
            Reload();
        }

        public static void Reload()
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
            if (path.Contains("/InternalResources/"))
            {
                return;
            }

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
            foreach (string path in importedAssets)
            {
                Process(path);
            }

            foreach (string path in movedAssets)
            {
                Process(path);
            }

            foreach (string path in movedFromAssetPaths)
            {
                Process(path);
            }

        }
#endif


    }
}
