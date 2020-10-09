using SwiftFramework.EditorUtils;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using SwiftFramework.Helpers;
using UnityEditor.Compilation;
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

        private void OnFocus()
        {
            pluginsData.Clear();
        }

        public void OnGUI()
        {
            Color defaultColor = GUI.color;

            if (PluginsManifest.Instance == null)
            {
                EditorGUILayout.LabelField("PluginsManifest not found.", EditorGUIEx.BoldCenteredLabel);
                if (GUILayout.Button("Create"))
                {
                    PluginsManifest.Create();
                    Repaint();
                }
                return;
            }
            
            if (PluginsManifest.Instance.CurrentStage != InstallStage.None)
            {
                EditorGUILayout.LabelField(PluginsManifest.GetInstallStageDescription(PluginsManifest.Instance.CurrentStage), EditorGUIEx.BoldCenteredLabel);

                if (GUILayout.Button("Cancel"))
                {
                    PluginsManifest.Instance.CurrentStage = InstallStage.None;
                    Repaint();
                }

                return;
            }
            
            this.ShowCompileAndPlayModeWarning(out bool canEdit);
            
            if (canEdit == false)
            {
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Separator();

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
                    var data = PluginsManifest.Instance.GetPluginData(plugins[i]);
                    pluginsData.Add(plugin, data);
                }
            }

            foreach (var item in pluginsData.ToArray())
            {
                PluginInfo plugin = item.Key;
                PluginData data = item.Value;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                string desc = $"{plugin.description} : {plugin.displayVersion}";
                
                bool isInstallationValid = true;
                PluginInfo.ErrorSummary installationErrors = null;

                if (data == null)
                {
                    Color c = GUI.color;
                    GUI.color = EditorGUIEx.WarningRedColor;
                    EditorGUILayout.LabelField("Plugin data not found: " + plugin.name);
                    GUI.color = c;
                    continue;
                }
                
                if (data.installed)
                {
                    isInstallationValid = plugin.IsInstallationValid(out installationErrors);
                }

                GUI.color = isInstallationValid ? defaultColor : EditorGUIEx.WarningRedColor;
                
                plugin.showInfo =
                    EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width - INSTALL_BTN_WIDTH, lineHeight),
                        plugin.showInfo, desc, true);

                GUI.color = defaultColor;
                
                if (plugin.showInfo)
                {
                    EditorGUI.indentLevel++;
                    
                    plugin.showDependencies = EditorGUILayout.Foldout(plugin.showDependencies, "Dependencies", true);

                    if (plugin.showDependencies)
                    {
                        EditorGUI.indentLevel++;
                        if (plugin.HasDependencies)
                        {
                            EditorGUILayout.LabelField("Plugins:");
                            EditorGUI.indentLevel++;
                            foreach (PluginInfo dependency in plugin.GetDependencies())
                            {
                                if (dependency == null)
                                {
                                    Color c = GUI.color;
                                    GUI.color = EditorGUIEx.WarningRedColor;
                                    EditorGUILayout.LabelField("Invalid dependency!");
                                    GUI.color = c;
                                    continue;
                                }
                                Rect labelRect = EditorGUILayout.GetControlRect();
                                EnableDependencyWarning(PluginDependencyType.Plugin, installationErrors, AssetDatabase.GetAssetPath(dependency),
                                    data.installed, defaultColor, labelRect);
                                
                                EditorGUI.LabelField(labelRect, dependency.description + " : " + dependency.displayVersion);

                                GUI.color = defaultColor;
                            }
                            EditorGUI.indentLevel--;
                        }
                        
                        if (plugin.HasPackageDependencies)
                        {
                            EditorGUILayout.LabelField("Packages:");
                            EditorGUI.indentLevel++;
                            foreach (PluginInfo.PackageDependency dependency in plugin.GetPackages())
                            {
                                Rect labelRect = EditorGUILayout.GetControlRect();
                                EnableDependencyWarning(PluginDependencyType.Package, installationErrors, dependency.FullName,
                                    data.installed, defaultColor, labelRect);
                                EditorGUI.LabelField(labelRect, dependency.FullName);
                                GUI.color = defaultColor;
                            }
                            EditorGUI.indentLevel--;
                        }
                        
                        if (plugin.HasModules)
                        {
                            EditorGUILayout.LabelField("Modules:");
                            EditorGUI.indentLevel++;
                            foreach (ModuleInstallInfo info in plugin.GetModules())
                            {
                                Type interfaceType = info.GetInterfaceType();
    
                 
                                Rect labelRect = EditorGUILayout.GetControlRect();
                                EnableDependencyWarning(PluginDependencyType.Module, installationErrors, AssetDatabase.GetAssetPath(info),
                                    data.installed, defaultColor, labelRect);
                                EditorGUI.LabelField(labelRect,
                                    $"{interfaceType?.GetDisplayName()} ({info.GetModuleDescription()})");
                                GUI.color = defaultColor;
                            }
                            EditorGUI.indentLevel--;
                        }
                        
                        if (plugin.HasDefineSymbols)
                        {
                            EditorGUILayout.LabelField("Define Symbols:");
                            EditorGUI.indentLevel++;
                            foreach (var symbol in plugin.GetSymbols())
                            {
                                Rect labelRect = EditorGUILayout.GetControlRect();
                                EnableDependencyWarning(PluginDependencyType.DefineSymbol, installationErrors, symbol.symbolName,
                                    data.installed, defaultColor, labelRect);
                                EditorGUI.LabelField(labelRect, symbol.symbolName);
                                GUI.color = defaultColor;
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }

                    if (plugin.HasOptions())
                    {
                        plugin.showOptions = EditorGUILayout.Foldout(plugin.showOptions, "Options", true);

                        if (plugin.showOptions)
                        {
                            EditorGUI.indentLevel++;
                            plugin.DrawOptions(Repaint, data);
                            EditorGUI.indentLevel--;
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                    
                }
                
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
                }
                
                if (GUI.changed)
                {
                    EditorUtility.SetDirty(plugin);
                }
                
                GUI.color = color;
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
        }

        private static void Refresh()
        {
            foreach (PluginInfo pluginInfo in plugins)
            {
                pluginInfo.Refresh();
            }
            AssetsUtil.TriggerPostprocessEvent();
            plugins.Clear();
            pluginsData.Clear();
        }

        private void EnableDependencyWarning(PluginDependencyType type, PluginInfo.ErrorSummary summary, string id, bool installed, Color defaultColor, Rect rect)
        {
            if (summary == null)
            {
                return;
            }
            if (summary.Contains(id, type))
            {
                GUI.color = EditorGUIEx.WarningRedColor;
    
                if (GUI.Button(new Rect(rect.x + rect.width - 90, rect.y, 90, rect.height),  "Resolve"))
                {
                    ResolveDependency(type, id);
                }
            }
            else
            {
                GUI.color = installed ? EditorGUIEx.GreenColor : defaultColor;
            }
        }

        private void ResolveDependency(PluginDependencyType type, string id)
        {
            switch (type)
            {
                case PluginDependencyType.Plugin:
                    Install(AssetDatabase.LoadAssetAtPath<PluginInfo>(id));
                    break;
                case PluginDependencyType.DefineSymbol:
                    DefineSymbols.Add(id, "");
                    break;
                case PluginDependencyType.Package:
                    string[] package = id.Split(':');
                    Util.AddDependencyToPackageManifest(package[0], package[1]);
                    break;
                case PluginDependencyType.Module:
                    ModuleInstaller.Install(AssetDatabase.LoadAssetAtPath<ModuleInstallInfo>(id).GenerateLink());
                    break;
            }
            EditorApplication.delayCall += Refresh;
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
            PluginsManifest.Instance.BeginRemoval(plugin);
            
            Repaint();

            PluginData data = PluginsManifest.Instance.GetPluginData(plugin);

            if (data == null)
            {
                return;
            }
            
            bool finishOnRecompile = false;

            foreach (string file in data.copiedFiles)
            {
                if (AssetDatabase.IsValidFolder(file))
                {
                    continue;
                }
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

            DefineSymbols.Disable(plugin.GetSymbols());

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
            
            PluginData data = PluginsManifest.Instance.GetPluginData(PluginsManifest.Instance.CurrentPlugin);
            data.installed = true;
            data.version = PluginsManifest.Instance.CurrentPlugin.version;
            EditorUtility.SetDirty(PluginsManifest.Instance);
            
            PluginsManifest.Instance.FinishInstallation();

            if (PluginsManifest.Instance.TryGetPluginFromInstallQueue(out PluginInfo pluginFromQueue))
            {
                Install(pluginFromQueue);
            }
            else
            {
                EditorApplication.delayCall += Refresh;
            }
        }

        private static void InstallModules()
        {
            PluginInfo pluginInfo = PluginsManifest.Instance.CurrentPlugin;

            foreach (ModuleInstallInfo moduleInstaller in pluginInfo.GetModules())
            {
                InstallModule(moduleInstaller);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void InstallModule(ModuleInstallInfo moduleInstallInfo)
        {
            ModuleInstaller.Install(moduleInstallInfo.GenerateLink());
        }


        private static void FinishUninstalling(bool compiled)
        {
            PluginsManifest.Instance.FinishUninstall();
            EditorApplication.delayCall += Refresh;
        }

        private static bool CopyFiles()
        {
            bool copiedScripts = false;
            
            foreach (DirectoryInfo subDir in PluginsManifest.Instance.CurrentPlugin.RootDirectory.GetDirectories())
            {
                string targetFolder = subDir.Name;

                if (targetFolder == "_Modules")
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

                foreach (string copiedFile in Util.CopyDirectory(subDir.FullName, Application.dataPath + "/" + targetFolder, ".meta"))
                {
                    if (copiedFile.EndsWith(".cs"))
                    {
                        copiedScripts = true;
                    }
                }
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (copiedScripts)
            {
                CompilationPipeline.RequestScriptCompilation();
            }

            return copiedScripts;
        }

        public static void Install(PluginInfo plugin)
        {
            foreach (PluginInfo pluginDependency in plugin.GetDependencies())
            {
                if (PluginsManifest.Instance.GetPluginData(pluginDependency).installed == false)
                {
                    PluginsManifest.Instance.AddPluginToInstallQueue(plugin);
                    Install(pluginDependency);
                    return;
                }
            }
            
            PluginsManifest.Instance.BeginInstallation(plugin);

            if (plugin.CanInstall() == false)
            {
                FinishInstalling(false);
                return;
            }

            plugin.OnWillInstall();

            ProceedInstall(true);
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static PluginInstaller()
        {
            AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
        }
        
        public static void ProceedInstall(bool compiled)
        {
            PluginsManifest.Instance.CurrentStage = (InstallStage)((int) PluginsManifest.Instance.CurrentStage + 1);
            switch (PluginsManifest.Instance.CurrentStage)
            {
                case InstallStage.InstallingUnityPackages:
                    if (InstallUnityPackages())
                    {
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
                    if (DefineSymbols.Add(PluginsManifest.Instance.CurrentPlugin.GetSymbols()))
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
                case InstallStage.InstallingModules:
                    InstallModules();
                    ProceedInstall(true);
                    break;
                case InstallStage.Finished:

                    if (HasToRecompileOnFinish())
                    {
                        CompilationPipeline.RequestScriptCompilation();
                        Compile.OnFinishedCompile += FinishInstalling;
                    }
                    else
                    {
                        FinishInstalling(true);
                    }
                    break;
            }
        }

        private static void OnPackageImportCompleted(string package)
        {
            if (PluginsManifest.Instance.CurrentStage == InstallStage.InstallingUnityPackages)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                
                if (EditorApplication.isCompiling)
                {
                    Compile.OnFinishedCompile += ProceedInstall;
                }
                else
                {
                    ProceedInstall(true);
                }
            }
        }

        private static bool HasToRecompileOnFinish()
        {
            return false;
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
                AssetDatabase.ImportPackage(PathUtils.ToRelativePath(unityPackage), false);
                return true;
            }

            return false;
        }


        [MenuItem("SwiftFramework/Plugins")]
        public static void OpenWindow()
        {
            pluginsData.Clear();
            PluginInstaller win = GetWindow<PluginInstaller>("Plugin Installer", true);
            win.minSize = new Vector2(400, win.minSize.y);
        }

    }

}
