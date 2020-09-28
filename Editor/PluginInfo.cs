using System;
using System.Collections.Generic;
using System.IO;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Plugin Info")]
    public class PluginInfo : ScriptableObject
    {
        public DirectoryInfo RootDirectory => new FileInfo(AssetDatabase.GetAssetPath(this)).Directory;
        public string description;
        public int priority;
        public int version;
        public string displayVersion;
        public bool canBeRemoved;
        public bool canBeUpdated;
        public AssemblyLinkerInfo[] linkAssemblies = {};
        
        [SerializeField] protected string[] defineSymbols;
        [SerializeField] protected PackageDependency[] packageDependencies;
        [SerializeField] protected PluginInfo[] dependencies;
        [SerializeField] protected ModuleInstallInfo[] modules;
        
        
        [HideInInspector] public bool showInfo = false;
        [HideInInspector] public bool showDependencies = false;
        [HideInInspector] public bool showOptions = false;

        private ErrorSummary errorSummary = null;

        public virtual bool CanInstall() => true;
        
        public virtual bool CanRemove() => canBeRemoved;

        public virtual bool HasModules => modules.Length > 0;
        public virtual bool HasDependencies => dependencies.Length > 0;
        public virtual bool HasPackageDependencies => packageDependencies.Length > 0;
        public virtual bool HasDefineSymbols => defineSymbols.Length > 0;

        public virtual IEnumerable<ModuleInstallInfo> GetModules() => modules;
        public virtual IEnumerable<PluginInfo> GetDependencies() => dependencies;
        public virtual IEnumerable<PackageDependency> GetPackages() => packageDependencies;

        public virtual void OnWillInstall()
        {
            
        }

        public virtual void OnUpdate(int oldVersion, int newVersion)
        {
        }
        
        public class ErrorSummary
        {
            public class DependencyError
            {
                public PluginDependencyType type;
                public readonly HashSet<string> invalidItems = new HashSet<string>();
            }
            
            public readonly List<DependencyError> errors = new List<DependencyError>();

            public bool Contains(string id, PluginDependencyType type)
            {
                foreach (DependencyError error in errors)
                {
                    if (error.type == type && error.invalidItems.Contains(id))
                    {
                        return true;
                    }
                }
                return false;
            }
            
            public bool IsEmpty()
            {
                foreach (DependencyError dependencyError in errors)
                {
                    if (dependencyError.invalidItems.Count > 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            public void RegisterInvalidItem(string id, PluginDependencyType type)
            {
                DependencyError error = errors.Find(e => e.type == type);
                if (error == null)
                {
                    error = new DependencyError()
                    {
                        type = type
                    };
                    errors.Add(error);
                }
                
                error.invalidItems.Add(id);
            }
        }

        public bool Installed
        {
            get
            {
                PluginData data = PluginsManifest.Instance.GetPluginData(this);
                if (data == null)
                {
                    return false;
                }
                return data.installed;
            }
        }

        public bool IsInstallationValid(out ErrorSummary errors)
        {
            if (errorSummary != null)
            {
                errors = errorSummary;
                return errors.IsEmpty();
            }
            
            errors = new ErrorSummary();
            errorSummary = errors;

            PluginData data = PluginsManifest.Instance.GetPluginData(this);

            if (data.installed == false)
            {
                return false;
            }

            foreach (var symbol in GetSymbols())
            {
                if (DefineSymbols.Instance.IsEnabled(symbol.symbolName) == false)
                {
                    errors.RegisterInvalidItem($"{symbol.symbolName}", PluginDependencyType.DefineSymbol);
                }
            }
            
            foreach (ModuleInstallInfo moduleInfo in GetModules())
            {
                if (ModuleInstaller.IsModuleInstallationValid(moduleInfo) == false)
                {
                    errors.RegisterInvalidItem($"{AssetDatabase.GetAssetPath(moduleInfo)}", PluginDependencyType.Module);
                }
            }
            
            foreach (PackageDependency packageDependency in GetPackages())
            {
                if (Util.HasPackageDependency(packageDependency.packageName, packageDependency.version) == false)
                {
                    errors.RegisterInvalidItem($"{packageDependency.FullName}", PluginDependencyType.Package);
                }
            }
            
            foreach (PluginInfo dependency in GetDependencies())
            {
                if (dependency == this)
                {
                    Debug.LogWarning($"Circular dependency: {description} depends on itself!");
                    continue;
                }
                if (dependency == null)
                {
                    Debug.LogWarning($"Empty dependency detected on {description}!");
                    continue;
                }
                if (dependency.IsInstallationValid(out var depErrors) == false)
                {
                    errors.RegisterInvalidItem($"{AssetDatabase.GetAssetPath(dependency)}", PluginDependencyType.Plugin);
                }
            }
            
            return errors.IsEmpty();
        }
        
        public virtual void OnRemoved()
        {
        }

        public virtual IEnumerable<(string symbolName, string symbolDesc)> GetSymbols()
        {
            foreach (var item in defineSymbols)
            {
                yield return (item, description);
            }
        }
        
        public virtual void DrawOptions(Action repaintHandler, PluginData data)
        {
        }

        [Serializable]
        public class PackageDependency
        {
            public string packageName;
            public string version;
            public string scopedRegistries;
            public string FullName => $"{packageName}:{version}";
        }

        public virtual void FinishInstall()
        {
            
            
        }
        
        public virtual void FinishUninstall()
        {
            
            
        }

        public virtual bool HasOptions()
        {
            return false;
        }

        public void Refresh()
        {
            errorSummary = null;
        }
    }

    public enum PluginDependencyType
    {
        Plugin, 
        DefineSymbol, 
        Package, 
        Module
    }
}