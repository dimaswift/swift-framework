using System;
using System.Collections.Generic;
using Swift.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    public enum InstallStage
    {
        None = 0, 
        InstallingUnityPackages = 1, 
        ModifyingPackageManager = 2, 
        AddingDefines = 3, 
        CopyingFiles = 4,
        InstallingModules = 5,
        Finished = 6
    }

    public class PluginsManifest : ScriptableEditorSettings<PluginsManifest>
    {
        protected override bool AutoCreate => false;

        public PluginInfo CurrentPlugin => currentPlugin;

        public PluginData GetPluginData(PluginInfo pluginInfo)
        {
            string path = AssetDatabase.GetAssetPath(pluginInfo);
            PluginData data = pluginsData.Find(d => d.path == path);
            if (data == null)
            {
                data = new PluginData()
                {
                    path = AssetDatabase.GetAssetPath(pluginInfo),
                    installed = false,
                    name = pluginInfo.name,
                    version = 0
                };
                Instance.Add(data);
                EditorUtility.SetDirty(Instance);
            }
            return data;
        }

        public void BeginInstallation(PluginInfo pluginInfo)
        {
            assetsRegistryCache = "";
            assetsRegistry.Clear();
            foreach (string guid in AssetDatabase.FindAssets("", new string[] { "Assets" }))
            {
                assetsRegistryCache += guid + '\n';
            }
            currentPlugin = pluginInfo;
            EditorUtility.SetDirty(this);
        }

        public void BeginRemoval(PluginInfo pluginInfo)
        {
            currentPlugin = pluginInfo;
            EditorUtility.SetDirty(this);
        }
        
        public void FinishInstallation()
        {
            if (currentPlugin == null)
            {
                return;
            }

            PluginData data = Instance.GetPluginData(Instance.CurrentPlugin);
   
            EditorUtility.SetDirty(Instance);
            assetsRegistry.Clear();
            string[] assets = Instance.assetsRegistryCache.Split('\n');
            foreach (string asset in assets)
            {
                if (string.IsNullOrEmpty(asset) == false && assetsRegistry.Contains(asset) == false)
                {
                    assetsRegistry.Add(asset);
                }
            }
            
            HashSet<string> assetsAfterInstallation = new HashSet<string>(AssetDatabase.FindAssets("", new string[] {"Assets"}));

            foreach (string guid in assetsAfterInstallation)
            {
                if (assetsRegistry.Contains(guid) == false)
                {
                    data.copiedFiles.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            
            EditorUtility.SetDirty(currentPlugin);
            currentPlugin.FinishInstall();
            currentPlugin = null;
            EditorUtility.SetDirty(this);
        }

        public void FinishUninstall()
        {
            currentPlugin.FinishUninstall();
            currentPlugin = null;
            EditorUtility.SetDirty(this);
        }

        [SerializeField] private List<PluginData> pluginsData = new List<PluginData>();

        [SerializeField] private PluginInfo currentPlugin = null;

        [SerializeField] private InstallStage currentStage = InstallStage.None;
        
        [SerializeField] private List<PluginInfo> dependencyInstallQueue = new List<PluginInfo>();

        [SerializeField] private string assetsRegistryCache = null;
        
        private static readonly HashSet<string> assetsRegistry = new HashSet<string>();

        public IEnumerable<PluginData> GetPlugins()
        {
            return pluginsData;
        }
        
        public InstallStage CurrentStage
        {
            get => currentStage;
            set
            {
                currentStage = value;
                EditorUtility.SetDirty(this);
            }
        }

        public void AddPluginToInstallQueue(PluginInfo pluginInfo)
        {
            if (IsInstalled(pluginInfo))
            {
                return;
            }

            foreach (PluginInfo info in dependencyInstallQueue)
            {
                if (info == pluginInfo)
                {
                    return;
                }
            }
            
            dependencyInstallQueue.Add(pluginInfo);
            EditorUtility.SetDirty(this);
        }

        private bool IsInstalled(PluginInfo pluginInfo)
        {
            PluginData data = GetPluginData(pluginInfo);
            if (data == null)
            {
                return false;
            }
            return data.installed;
        }
        
        public bool TryGetPluginFromInstallQueue(out PluginInfo dependencyPlugin)
        {
            if (dependencyInstallQueue.Count == 0)
            {
                dependencyPlugin = null;
                return false;
            }

            dependencyPlugin = dependencyInstallQueue[0];
            dependencyInstallQueue.RemoveAt(0);
            EditorUtility.SetDirty(this);
            return true;
        }
        
        public static string GetInstallStageDescription(InstallStage stage)
        {
            switch (stage)
            {
                case InstallStage.None:
                    return null;
                case InstallStage.InstallingUnityPackages:
                    return "Importing unity packages...";
                case InstallStage.ModifyingPackageManager:
                    return "Installing packages...";
                case InstallStage.AddingDefines:
                    return "Setting up define symbols...";
                case InstallStage.CopyingFiles:
                    return "Copying files...";
                case InstallStage.Finished:
                    return "Finishing...";
                default:
                    return null;
            }
        }

        internal void Add(PluginData data)
        {
            if (pluginsData.Find(d => d.name == data.name && d.version == data.version) != null)
            {
                return;
            }
            pluginsData.Add(data);
            EditorUtility.SetDirty(this);
        }
    }


    [Serializable]
    public class PluginData
    {
        public string name;
        public string path;
        public bool installed;
        public int version;
        public List<string> copiedFiles = new List<string>();
    }
}