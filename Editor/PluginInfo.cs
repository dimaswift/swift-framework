using System;
using System.Collections.Generic;
using System.IO;
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
        public virtual void OnInstall()
        {
            PluginInstaller.FinishInstalling();
        }
        public virtual void OnUpdate(int oldVersion, int newVersion) { }

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

        public virtual void DrawCustomGUI(Action repaintHandler, PluginData data) { }

        [Serializable]
        public class PackageDependency
        {
            public string packageName;
            public string version;
        }
    }

}
