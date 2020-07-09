using System;
using System.Collections.Generic;
using System.IO;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Plugin Info")]
    public class PluginInfo : ScriptableObject
    {
        public DirectoryInfo RootDirectory => new FileInfo(AssetDatabase.GetAssetPath(this)).Directory;

        public int priority;
        public string[] defineSymbols;
        public string description;
        public PackageDependency[] packageDependencies;
        public int version;
        public string displayVersion;
        public bool canBeRemoved;
        public bool canBeUpdated;

        public virtual bool CanInstall() => true;
        public virtual bool CanRemove() => canBeRemoved;

        public BehaviourModuleFactory[] behaviourModules = { };
 
        [Serializable]
        public class BehaviourModuleFactory
        {
            public string type;
            public string name;
        }
        
        public virtual void OnWillInstall()
        {
            
        }

        public virtual void OnUpdate(int oldVersion, int newVersion)
        {
        }

        public virtual void OnRemoved()
        {
        }

        public IEnumerable<(string symbolName, string symbolDesc)> GetSymbols()
        {
            foreach (var item in defineSymbols)
            {
                yield return (item, description);
            }
        }

        public virtual void DrawCustomGUI(Action repaintHandler, PluginData data)
        {
        }

        [Serializable]
        public class PackageDependency
        {
            public string packageName;
            public string version;
        }
        
        public void FinishInstall()
        {
            
            foreach (BehaviourModuleFactory module in behaviourModules)
            {
                GameObject moduleObject = new GameObject(module.name);
                moduleObject.AddComponent(Type.GetType(module.type));
                string dir = ResourcesAssetHelper.RootFolder + "/Modules/";
                
                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                string prefabPath = dir + module.name + ".prefab";
                PluginsManifest.Instance.CurrentPluginData.copiedFiles.Add(prefabPath);
                EditorUtility.SetDirty(PluginsManifest.Instance);
                PrefabUtility.SaveAsPrefabAsset(moduleObject, prefabPath);
            }
        }
    }
}