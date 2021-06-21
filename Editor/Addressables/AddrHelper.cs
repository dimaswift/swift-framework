#if USE_ADDRESSABLES

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Swift.EditorUtils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Swift.Core.Editor
{
    [InitializeOnLoad]
    internal class AddrHelper : AssetPostprocessor
    {
        public static event Action OnReload = () => { };

        private static readonly List<AddressableAssetEntry> assets = new List<AddressableAssetEntry>();

        private static bool reloading;

        private static readonly List<Type> prewarmedInterfaces = new List<Type>();

        internal const string ROOT_FOLDER = "Assets/Addressables";

        private static readonly string[] rootFolders = {ROOT_FOLDER};

        public static bool ReloadOnPostProcess { get; set; }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (ReloadOnPostProcess)
            {
                EditorApplication.delayCall += Reload;
                ReloadOnPostProcess = false;
            }
        }


        private static AddressableAssetSettings Settings
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
            foreach (AddressableAssetEntry entry in assets)
            {
                Object asset = AssetDatabase.LoadAssetAtPath(entry.AssetPath, type);
                if (asset != null)
                {
                    yield return entry;
                }
            }
        }

        static AddrHelper()
        {
            EditorApplication.delayCall += Reload;
        }
        
        public static IEnumerable<AddressableAssetEntry> GetPrefabsWithComponent(Type component)
        {
            foreach (AddressableAssetEntry entry in assets)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                if (go != null && go.GetComponent(component) != null)
                {
                    yield return entry;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetScriptableObjectsWithInterface(Type @interface)
        {
            foreach (AddressableAssetEntry entry in assets)
            {
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath);
                if (so != null && @interface.IsInstanceOfType(so))
                {
                    yield return entry;
                }
            }
        }

        public static IEnumerable<AddressableAssetEntry> GetScenes()
        {
            foreach (AddressableAssetEntry entry in assets)
            {
                SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.AssetPath);
                if (asset != null)
                {
                    yield return entry;
                }
            }
        }

        public static void ProcessMove(string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetEntry entry = Settings.FindAssetEntry(guid);
            if (entry != null)
            {
                Settings.RemoveAssetEntry(guid);
                entry = Settings.CreateOrMoveEntry(guid, Settings.DefaultGroup);
                entry.SetAddress(NormalizeAddress(path));
                Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
                Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true);
            }
        }

        public static AddressableAssetEntry FindEntry(string key)
        {
            foreach (AddressableAssetGroup group in Settings.groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry.address == key)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        private static string NormalizeAddress(string address)
        {
            return address.Substring(ROOT_FOLDER.Length + 1, address.Length - (ROOT_FOLDER.Length + 1))
                .RemoveExtention();
        }

#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Reload")]
#endif
        private static void ManualReload()
        {
            Util.PromptMoveAssetsFromFolder("Resources", "Addressables");
            reloading = false;
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

            foreach (Type type in Util.GetAllTypes())
            {
                if (type.IsInterface && type.GetCustomAttribute<PrewarmAssetAttribute>() != null)
                {
                    prewarmedInterfaces.Add(type);
                }
            }

            foreach (string rootFolder in rootFolders)
            {
                Util.EnsureProjectFolderExists(rootFolder);
                if (AssetDatabase.IsValidFolder(rootFolder) == false)
                {
                    reloading = false;
                    return;
                }
            }


            foreach (string guid in AssetDatabase.FindAssets("", rootFolders))
            {
                CreateOrModifyEntry(guid);
            }

            assets.Clear();
            foreach (AddressableAssetGroup group in Settings.groups.ToArray())
            {
                if (!group || group.entries.Count == 0)
                {
                    Settings.RemoveGroup(group);
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
                if (go && go.GetComponent<BehaviourModule>() != null)
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
                
                if (group.HasSchema<ContentUpdateGroupSchema>() == false)
                {
                    group.AddSchema<ContentUpdateGroupSchema>();
                }
              
                if (group.HasSchema<BundledAssetGroupSchema>() == false)
                {
                    group.AddSchema<BundledAssetGroupSchema>();
                }
               
                EditorUtility.SetDirty(group);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            return group;
        }

        public static AddressableAssetEntry CreateOrModifyEntry(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

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
                foreach (string label in labelAttr.labels)
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

            foreach (Type prewarmedInterface in prewarmedInterfaces)
            {
                if (prewarmedInterface.IsInstanceOfType(asset))
                {
                    Settings.AddLabel(AddrLabels.Prewarm, true);
                    entry.labels.Add(AddrLabels.Prewarm);
                    break;
                }
            }

            if (asset.TryGetAttribute(out Type singletonType, out AddrSingletonAttribute singleton))
            {
                if (AssetCache.TryGetSingletonAddress(singletonType, out string addr))
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
    }
}
#endif