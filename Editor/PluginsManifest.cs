using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
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

        public void StartProcess(PluginInfo pluginInfo)
        {
            currentPlugin = pluginInfo;
            EditorUtility.SetDirty(this);
        }

        public void FinishProcess()
        {
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