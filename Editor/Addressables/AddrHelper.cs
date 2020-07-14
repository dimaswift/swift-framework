#if USE_ADDRESSABLES

using SwiftFramework.Core.Editor;
using SwiftFramework.EditorUtils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditorInternal;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [InitializeOnLoad]
    internal class AddrHelper : AssetPostprocessor
    {
        public static event Action OnReload = () => { };

        private static List<AddressableAssetEntry> assets = new List<AddressableAssetEntry>();

        private static bool reloading;

        private static List<Type> prewarmedInterfaces = new List<Type>();

        internal const string ROOT_FOLDER = "Assets/Addressables";

        public static readonly string[] rootFolders = { ROOT_FOLDER };

        public static AddressableAssetSettings Settings
        {
            get
            {
                if (settings != null)
                {
                    return settings;
                }
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                return settings;
            }
        }

        private static AddressableAssetSettings settings;

        public static IEnumerable<AddressableAssetEntry> GetAssets(Func<AddressableAssetEntry, bool> filter)
        {
            foreach (var asset in assets)
            {
                if (filter(asset))
                {
                    yield return asset;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetAssets(Type type)
        {
            foreach (var entry in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath(entry.AssetPath, type);
                if (asset != null)
                {
                    yield return entry;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetPrefabsWithComponent(Type component)
        {
            foreach (var entry in assets)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                if (go != null && go.GetComponent(component) != null)
                {
                    yield return entry;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetScriptableObjectsWithInterface(Type @interface)
        {
            foreach (var entry in assets)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath);
                if (so != null && @interface.IsAssignableFrom(so.GetType()))
                {
                    yield return entry;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetScenes()
        {
            foreach (var entry in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.AssetPath);
                if (asset != null)
                {
                    yield return entry;
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in movedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                var entry = Settings.FindAssetEntry(guid);
                if (entry != null)
                {
                    var oldAddress = entry.address;
                    Settings.RemoveAssetEntry(guid);
                    entry = Settings.CreateOrMoveEntry(guid, Settings.DefaultGroup);
                    entry.SetAddress(NormalizeAddress(path));
                    Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                    Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
                    Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true);
                }
            }
            AddressableAssetSettings.OnModificationGlobal -= AddressableAssetSettings_OnModificationGlobal;
            AddressableAssetSettings.OnModificationGlobal += AddressableAssetSettings_OnModificationGlobal;
            Reload();
        }

        private static void AddressableAssetSettings_OnModificationGlobal(AddressableAssetSettings arg1, AddressableAssetSettings.ModificationEvent arg2, object arg3)
        {
            Reload();
        }

        public static AddressableAssetEntry FindEntry(string key)
        {
            foreach (var group in Settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == key)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        public static string NormalizeAddress(string address)
        {
            return address.Substring(ROOT_FOLDER.Length + 1, address.Length - (ROOT_FOLDER.Length + 1)).RemoveExtention();
        }

        private static bool IsInsideRootFolder(string path)
        {
            foreach (var f in rootFolders)
            {
                if (path.StartsWith(f))
                {
                    return true;
                }
            }
            return false;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Reload();
        }
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Reload")]
#endif
        private static void ManualReload()
        {
            Util.PromptMoveAssetsFromFolder("Resources", "Addressables");
          
            Reload(); 
        }

        public static void Reload()
        {
            if (Directory.Exists(ROOT_FOLDER) == false)
            {
                Directory.CreateDirectory(ROOT_FOLDER);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (reloading)
            {
                return;
            }
            reloading = true;

            prewarmedInterfaces.Clear();

            foreach (var type in Util.GetAllTypes())
            {
                if (type.IsInterface && type.GetCustomAttribute<PrewarmAssetAttribute>() != null)
                {
                    prewarmedInterfaces.Add(type);
                }
            }

            foreach (string rootFolder in rootFolders)
            {
                Util.EnsureProjectFolderExists(rootFolder);
            }

            foreach (string guid in AssetDatabase.FindAssets("", rootFolders))
            {
                CreateOrModifyEntry(guid);
            }

            assets.Clear();
            for (int i = 0; i < Settings.groups.Count; i++)
            {
                if (!Settings.groups[i] || Settings.groups[i].entries.Count == 0)
                {
                    Settings.RemoveGroup(Settings.groups[i]);
                }
            }
            Settings.GetAllAssets(assets, false);
            OnReload();
            reloading = false;
        }

        private static AddressableAssetGroup GetAssetGroup(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return GetOrCreateGroup(AddrGroups.Default);
            }

            Type assetType = asset.GetType();

            if (assetType == typeof(BootConfig))
            {
                return GetOrCreateGroup(AddrGroups.Boot);
            }

            if (asset.TryGetAttribute(out Type type, out AddrGroupAttribute groupAttr))
            {
                return GetOrCreateGroup(groupAttr.groupName);
            }

            if (assetType == typeof(Sprite) || assetType == typeof(Texture2D))
            {
                return GetOrCreateGroup(AddrGroups.Images);
            }

            if (assetType == typeof(GameObject))
            {
                GameObject go = asset as GameObject;
                if (go.GetComponent<BehaviourModule>() != null)
                {
                    return GetOrCreateGroup(AddrGroups.Modules);
                }
                return GetOrCreateGroup(AddrGroups.Views);
            }

            if (typeof(ScriptableObject).IsAssignableFrom(assetType))
            {
                return GetOrCreateGroup(AddrGroups.Configs);
            }


            if (assetType == typeof(AudioClip))
            {
                return GetOrCreateGroup(AddrGroups.Sounds);
            }

            return GetOrCreateGroup(AddrGroups.Default);
        }

        private static AddressableAssetGroup GetOrCreateGroup(string name)
        {
            var group = Settings.FindGroup(name);
            if (group == null)
            {
                group = Util.CreateScriptable<AddressableAssetGroup>(name, "Assets/AddressableAssetsData/AssetGroups");
                group.Name = name;
                EditorUtility.SetDirty(group);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            return group;
        }

        public static AddressableAssetEntry CreateOrModifyEntry(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            var entry = Settings.FindAssetEntry(guid);

            if (entry == null)
            {
                AddressableAssetGroup group = GetAssetGroup(asset);
                entry = Settings.CreateOrMoveEntry(guid, group);
                Settings.MoveEntry(entry, group);
            }

            entry.SetAddress(NormalizeAddress(path));

            if (asset == null)
            {
                return entry;
            }

            if (asset.TryGetAttribute(out Type labeledType, out AddrLabelAttribute labelAttr))
            {
                foreach (var label in labelAttr.labels)
                {
                    Settings.AddLabel(label, true);
                    if (entry.labels.Contains(label) == false)
                    {
                        entry.labels.Add(label);
                    }
                }
            }

            if (asset.TryGetAttribute(out Type prewarmedType, out PrewarmAssetAttribute prewarm))
            {
                Settings.AddLabel(AddrLabels.Prewarm, true);
                entry.labels.Add(AddrLabels.Prewarm);
            }

            foreach (var prewarmedInterface in prewarmedInterfaces)
            {
                if (prewarmedInterface.IsAssignableFrom(asset.GetType()))
                {
                    Settings.AddLabel(AddrLabels.Prewarm, true);
                    entry.labels.Add(AddrLabels.Prewarm);
                    break;
                }
            }

            if (asset.TryGetAttribute(out Type singletonType, out AddrSingletonAttribute singleton))
            {
                if(AssetCache.TryGetSingletonAddress(singletonType, out string addr))
                {
                    entry.SetAddress(addr);
                }
            }

            if (asset.TryGetAttribute(out Type t, out WarmUpInstanceAttribute warmUpAttr))
            {
                Settings.AddLabel(AddrLabels.Prewarm, true);
                entry.labels.Add(AddrLabels.Prewarm);
            }

            return entry;
        }

        public static T GetAsset<T>(Link link) where T : UnityEngine.Object
        {
            foreach (var g in Settings.groups)
            {
                foreach (var e in g.entries)
                {
                    if (e.address == link.GetPath())
                    {
                        return AssetDatabase.LoadAssetAtPath<T>(e.AssetPath);
                    }
                }
            }

            return null;
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

                drawerClass.BaseTypes.Add(new CodeTypeReference("LinkPropertyDrawer", new[] { new CodeTypeReference(type.BaseType.GetGenericArguments()[0]) }));

                drawerClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("CustomPropertyDrawer"), new CodeAttributeArgument(new CodeTypeOfExpression(type))));

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


            foreach (var c in classes)
            {
                FileInfo file = new FileInfo(c.Key);
                var dir = file.Directory.FullName + "/Editor";

                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }

                AssemblyDefinitionAsset assemblyDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(c.Key);

                if (assemblyDefinition != null)
                {
                    string assemblyName = Path.GetFileNameWithoutExtension(file.Name) + ".Editor";
                    string editorAssemblyPath = dir + "/" + assemblyName + ".asmdef";

                    if (File.Exists(editorAssemblyPath) == false)
                    {
                        var json = string.Format(@"{{
""name"": ""{0}"",
""references"": [
    ""SwiftFramework.Core"",
    ""SwiftFramework.Core.Editor"",
    ""{1}""
],
""includePlatforms"": [ ""Editor"" ],
""excludePlatforms"": [],
""allowUnsafeCode"": false,
""overrideReferences"": false,
""precompiledReferences"": [],
""autoReferenced"": true,
""defineConstraints"": [],
""versionDefines"": [],
""noEngineReferences"": false
}}", assemblyName, Path.GetFileNameWithoutExtension(file.Name));

                        File.WriteAllText(editorAssemblyPath, json);
                    }
                }

                string filePath = $"{dir}/LinkDrawers{file.Directory.Name}.cs";
                ScriptBuilder.SaveClassToDisc(c.Value, filePath, false);

            }

        }
    }

}
#endif

