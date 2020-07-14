using System;
using System.Collections.Generic;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public enum InstallStage
    {
        None = 0, 
        InstallingUnityPackages = 1, 
        ModifyingPackageManager = 2, 
        AddingDefines = 3, 
        CopyingFiles = 4,
        Finished
    }

    public class PluginsManifest : ScriptableEditorSettings<PluginsManifest>
    {
        public PluginInfo CurrentPlugin
        {
            get => currentPlugin;
            set
            {
                currentPlugin = value;
                EditorUtility.SetDirty(this);
            }
        }

        public PluginData FindData(PluginInfo pluginInfo)
        {
            var path = AssetDatabase.GetAssetPath(pluginInfo);
            return pluginsData.Find(d => d.path == path);
        }

        public void BeginInstallation(PluginInfo pluginInfo)
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
            currentPlugin.FinishInstall();
            currentPlugin = null;
            EditorUtility.SetDirty(this);
        }

        public PluginData CurrentPluginData
        {
            get
            {
                if (currentPlugin == null)
                {
                    return null;
                }

                var path = AssetDatabase.GetAssetPath(currentPlugin);
                return pluginsData.Find(d => d.path == path);
            }
        }

        public SerializedObject SerializedObject
        {
            get
            {
                if (serializedObject == null)
                {
                    serializedObject = new SerializedObject(this);
                }

                return serializedObject;
            }
        }

        [SerializeField] private List<PluginData> pluginsData = new List<PluginData>();

        [SerializeField] private PluginInfo currentPlugin = null;

        [NonSerialized] private SerializedObject serializedObject = null;

        [SerializeField] private InstallStage currentStage = InstallStage.None;
        
        [SerializeField] private List<PluginInfo> dependencyInstallQueue = new List<PluginInfo>();

        public InstallStage CurrentStage
        {
            get => currentStage;
            set
            {
                currentStage = value;
                EditorUtility.SetDirty(this);
            }
        }

        public void AddDependencyToQueue(PluginInfo pluginInfo)
        {
            if (IsInstalled(pluginInfo))
            {
                return;
            }
            dependencyInstallQueue.Add(pluginInfo);
            EditorUtility.SetDirty(this);
        }

        public bool IsInstalled(PluginInfo pluginInfo)
        {
            PluginData data = FindData(pluginInfo);
            if (data == null)
            {
                return false;
            }
            return data.installed;
        }
        
        
        
        public bool TryGetDependencyFromQueue(out PluginInfo dependencyPlugin)
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
                    return "Installed successfully";
                default:
                    return null;
            }
        }

        internal void Add(PluginData data)
        {
            pluginsData.Add(data);
            EditorUtility.SetDirty(this);
        }

        internal void ResetAll()
        {
            pluginsData.Clear();
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