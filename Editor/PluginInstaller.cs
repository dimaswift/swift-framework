using SwiftFramework.EditorUtils;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using UnityEditor.VersionControl;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core.Editor
{
    [InitializeOnLoad]
    public class PluginInstaller : EditorWindow
    {
        private static readonly List<PluginInfo> plugins = new List<PluginInfo>();
        private static readonly Dictionary<PluginInfo, PluginData> pluginsData = new Dictionary<PluginInfo, PluginData>();

        private const float INSTALL_BTN_WIDTH = 70;
        private const float DELETE_BTN_WIDTH = 20;

        private Vector2 scrollPos;

        public void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Separator();

            if (PluginsManifest.Instance.CurrentStage != InstallStage.None)
            {
                EditorGUILayout.LabelField(PluginsManifest.GetInstallStageDescription(PluginsManifest.Instance.CurrentStage), EditorGUIEx.BoldCenteredLabel);

                if (GUILayout.Button("Cancel"))
                {
                    PluginsManifest.Instance.CurrentStage = InstallStage.None;
                    Repaint();
                }

                EditorGUILayout.EndScrollView();

                return;
            }

            EditorGUILayout.LabelField("Plugins", EditorGUIEx.BoldCenteredLabel);

            EditorGUILayout.Separator();

            if (pluginsData.Count == 0)
            {
                plugins.Clear();
                plugins.AddRange(Util.GetAssets<PluginInfo>());
                plugins.Sort((p1, p2) => -p1.priority.CompareTo(p2.priority));
                for (int i = 0; i < plugins.Count; i++)
                {
                    var plugin = plugins[i];
                    var data = PluginsManifest.Instance.FindData(plugins[i]);
                    if (data == null)
                    {
                        data = new PluginData()
                        {
                            path = AssetDatabase.GetAssetPath(plugin),
                            installed = false,
                            name = plugin.name,
                            version = 0
                        };
                        PluginsManifest.Instance.Add(data);
                    }
                    pluginsData.Add(plugin, data);
                }
            }

            foreach (var item in pluginsData)
            {
                PluginInfo plugin = item.Key;
                PluginData data = item.Value;
                var lineHeight = EditorGUIUtility.singleLineHeight;

                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                string desc = $"{plugin.description} : {plugin.displayVersion}";
                GUI.Label(new Rect(rect.x, rect.y, rect.width - INSTALL_BTN_WIDTH, lineHeight), desc);

                Color color = GUI.color;

                if (data.installed)
                {
                    bool canUpdate = data.version != plugin.version && plugin.canBeUpdated;

                    GUI.color = !canUpdate ? EditorGUIEx.GreenColor : EditorGUIEx.YellowColor;

                    var label = canUpdate ? "Update" : "Installed";

                    GUI.enabled = canUpdate;

                    Rect buttonRect = new Rect(rect.x + (rect.width - INSTALL_BTN_WIDTH - DELETE_BTN_WIDTH), rect.y,
                        INSTALL_BTN_WIDTH, lineHeight);
                    
                    if (GUI.Button(buttonRect, label))
                    {
                        UpdatePlugin(plugin, data);
                    }

                    GUI.color = color;

                    GUI.enabled = plugin.CanRemove();

                    buttonRect = new Rect(rect.x + rect.width - DELETE_BTN_WIDTH, rect.y, DELETE_BTN_WIDTH, lineHeight);

                    if (GUI.Button(buttonRect, "X"))
                    {
                        if (EditorUtility.DisplayDialog("Warning", $"Remove {plugin.description}?", "Yes", "Cancel"))
                        {
                            RemovePlugin(plugin);
                        }
                    }

                    GUI.enabled = true;
                }
                else
                {
                    GUI.color = color;

                    Rect buttonRect = new Rect(rect.x + rect.width - INSTALL_BTN_WIDTH - DELETE_BTN_WIDTH, rect.y,
                        INSTALL_BTN_WIDTH + DELETE_BTN_WIDTH, lineHeight);

                    if (GUI.Button(buttonRect, "Install"))
                    {
                        Install(plugin);
                        Repaint();
                    }

                    plugin.DrawCustomGUI(Repaint, data);
                }

                GUI.color = color;
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdatePlugin(PluginInfo plugin, PluginData data)
        {
            plugin.OnUpdate(data.version, plugin.version);
            data.version = plugin.version;
            EditorUtility.SetDirty(PluginsManifest.Instance);
            Repaint();
        }

        private void RemovePlugin(PluginInfo plugin)
        {
            PluginsManifest.Instance.BeginInstallation(plugin);
            
            Repaint();

            PluginData data = PluginsManifest.Instance.FindData(plugin);

            bool finishOnRecompile = false;

            foreach (string file in data.copiedFiles)
            {
                AssetDatabase.DeleteAsset(file);
                if (!finishOnRecompile && file.EndsWith(".cs"))
                {
                    Compile.OnFinishedCompile += FinishUninstalling;
                    finishOnRecompile = true;
                }
            }

            data.installed = false;
            data.version = 0;
            data.copiedFiles.Clear();

            EditorUtility.SetDirty(PluginsManifest.Instance);

            SymbolCatalog.Disable(plugin.GetSymbols());

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Util.DeleteEmptyFolders();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            plugin.OnRemoved();
            
            Repaint();

            if (!finishOnRecompile)
            {
                FinishUninstalling(true);
            }
        }

        public static void FinishInstalling(bool compiled)
        {
            PluginsManifest.Instance.CurrentStage = InstallStage.None;
            
            if (PluginsManifest.Instance.CurrentPlugin == null)
            {
                return;
            }

            if (compiled)
            {
                PluginData data = PluginsManifest.Instance.FindData(PluginsManifest.Instance.CurrentPlugin);
                data.installed = true;
                data.version = PluginsManifest.Instance.CurrentPlugin.version;
                EditorUtility.SetDirty(PluginsManifest.Instance);
            }
            
            PluginsManifest.Instance.FinishInstallation();
        }

        private static List<ModuleInstallInfo> GetModuleInstallers(PluginInfo pluginInfo)
        {
            DirectoryInfo pluginRootDir = pluginInfo.RootDirectory;

            List<ModuleInstallInfo> moduleInstallers = new List<ModuleInstallInfo>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(ModuleInstallInfo).FullName}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains($"/{pluginRootDir.Name}/"))
                {
                    moduleInstallers.Add(AssetDatabase.LoadAssetAtPath<ModuleInstallInfo>(path));
                }
            }

            return moduleInstallers;
        }

        private static void InstallModules()
        {
            PluginInfo pluginInfo = PluginsManifest.Instance.CurrentPlugin;
            
            List<ModuleInstallInfo> moduleInstallers = GetModuleInstallers(pluginInfo);

            foreach (ModuleInstallInfo moduleInstaller in moduleInstallers)
            {
                InstallModule(moduleInstaller);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }


        private static void InstallModule(ModuleInstallInfo moduleInstallInfo)
        {
            ModuleManifest manifest = moduleInstallInfo.isCoreModule ? CreateInstance<CoreModuleManifest>() : CreateInstance<ModuleManifest>();
            manifest.name = moduleInstallInfo.name;
            SerializedObject so = new SerializedObject(manifest);
            SerializedProperty moduleProp = so.FindProperty("module");
            moduleProp.FindPropertyRelative("implementationType").stringValue =
                moduleInstallInfo.module.implementationType;
            moduleProp.FindPropertyRelative("interfaceType").stringValue =
                moduleInstallInfo.module.interfaceType;

            if (moduleInstallInfo.module.config)
            {
                moduleProp.FindPropertyRelative("configLink").FindPropertyRelative("Path").stringValue =
                    Folders.Configs + "/" + moduleInstallInfo.module.config.name;
                
                string folder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}/";
                string configPath = $"{folder}/{moduleInstallInfo.module.config.name}.asset";
                Util.EnsureProjectFolderExists(folder);
                
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(moduleInstallInfo.module.config), configPath);
                
                RegisterCopiedFile(configPath);
            }
            
            if (moduleInstallInfo.module.behaviour)
            {
                string folder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Behaviours}/";
                
                Util.EnsureProjectFolderExists(folder);
                string behaviourPath = $"{folder}/{moduleInstallInfo.module.behaviour.name}.prefab";
                moduleProp.FindPropertyRelative("behaviourLink").FindPropertyRelative("Path").stringValue =
                    Folders.Behaviours + "/" + moduleInstallInfo.module.behaviour.name;
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(moduleInstallInfo.module.behaviour), behaviourPath);
                
                RegisterCopiedFile(behaviourPath);
            }
            
            so.ApplyModifiedProperties();
            string manifestFolder = ResourcesAssetHelper.RootFolder + "/Modules/";
            Util.EnsureProjectFolderExists(manifestFolder);
            string manifestPath = ResourcesAssetHelper.RootFolder + "/Modules/" + manifest.ImplementationType.Name +
                                  ".asset";
            AssetDatabase.CreateAsset(manifest, manifestPath);
            RegisterCopiedFile(manifestPath);
        }
        
        public static void RegisterCopiedFile(string localPath)
        {
            PluginData data = PluginsManifest.Instance.FindData(PluginsManifest.Instance.CurrentPlugin);
            data.copiedFiles.Add(localPath);
            EditorUtility.SetDirty(PluginsManifest.Instance);
        }
        
        private static void FinishUninstalling(bool compiled)
        {
            PluginsManifest.Instance.FinishInstallation();
        }

        private static bool CopyFiles()
        {
            PluginData data = PluginsManifest.Instance.FindData(PluginsManifest.Instance.CurrentPlugin);
            data.copiedFiles.Clear();
            foreach (DirectoryInfo subDir in PluginsManifest.Instance.CurrentPlugin.RootDirectory.GetDirectories())
            {
                string targetFolder = subDir.Name;

                if (targetFolder == "ModuleInstallers")
                {
                    continue;
                }

                if (targetFolder == "_Resources")
                {
                    targetFolder = ResourcesAssetHelper.RootFolderName;
                }
                
                if (targetFolder.StartsWith("_"))
                {
                    targetFolder = targetFolder.Substring(1, targetFolder.Length - 1);
                }
                data.copiedFiles.AddRange(Util.CopyDirectory(subDir.FullName, Application.dataPath + "/" + targetFolder, ".meta"));
            }

            for (int i = 0; i < data.copiedFiles.Count; i++)
            {
                if (data.copiedFiles[i].StartsWith("Assets") == false)
                {
                    data.copiedFiles[i] = Util.ToRelativePath(data.copiedFiles[i]);
                }
            }
            
            InstallModules();
            
            EditorUtility.SetDirty(PluginsManifest.Instance);
            
            return data.copiedFiles.FindIndex(c => c.EndsWith(".cs")) != -1;
        }

        private void Install(PluginInfo plugin)
        {
            PluginsManifest.Instance.BeginInstallation(plugin);

            if (plugin.CanInstall() == false)
            {
                FinishInstalling(false);
                Repaint();
                return;
            }

            plugin.OnWillInstall();

            ProceedInstall(true);
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Repaint();
        }

        public static void ProceedInstall(bool compiled)
        {
            PluginsManifest.Instance.CurrentStage = (InstallStage)((int) PluginsManifest.Instance.CurrentStage + 1);
            
            switch (PluginsManifest.Instance.CurrentStage)
            {
                case InstallStage.InstallingUnityPackages:
                    if (InstallUnityPackages())
                    {
                        Compile.OnFinishedCompile += ProceedInstall;
                    }
                    else
                    {
                        ProceedInstall(true);
                    }
                    break;
                case InstallStage.ModifyingPackageManager:
                    if (InstallToPackageManager())
                    {
                        Compile.OnFinishedCompile += ProceedInstall;
                    }
                    else
                    {
                        ProceedInstall(true);
                    }
                    break;
                case InstallStage.AddingDefines:
                    if (SymbolCatalog.Add(PluginsManifest.Instance.CurrentPlugin.GetSymbols()))
                    {
                        Compile.OnFinishedCompile += ProceedInstall;
                    }
                    else
                    {
                        ProceedInstall(true);
                    }
                    break;
                case InstallStage.CopyingFiles:
                    if (CopyFiles())
                    {
                        Compile.OnFinishedCompile += ProceedInstall;
                    }
                    else
                    {
                        ProceedInstall(true);
                    }
                    break;
                case InstallStage.Finished:
                    FinishInstalling(true);
                    break;
            }
        }

        private static bool InstallToPackageManager()
        {
            bool addedPackage = false;
            
            foreach (PluginInfo.PackageDependency dep in PluginsManifest.Instance.CurrentPlugin.GetPackages())
            {
                if (Util.AddDependencyToPackageManifest(dep.packageName, dep.version))
                {
                    addedPackage = true;
                }
            }

            return addedPackage;
        }
        
        private static bool InstallUnityPackages()
        {
            string unityPackage = null;

            DirectoryInfo pluginRootDir = PluginsManifest.Instance.CurrentPlugin.RootDirectory;

            foreach (FileInfo item in pluginRootDir.GetFiles())
            {
                if (item.Extension == ".unitypackage")
                {
                    unityPackage = item.FullName;
                    break;
                }
            }

            if (unityPackage != null)
            {
                AssetDatabase.ImportPackage(Util.ToRelativePath(unityPackage), false);
                return true;
            }

            return false;
        }
        
        [MenuItem("SwiftFramework/Plugins")]
        public static void Install()
        {
            pluginsData.Clear();

            PluginInstaller win = GetWindow<PluginInstaller>(true, "Plugin Installer", true);

            win.MoveToCenter();
        }

    }

}
