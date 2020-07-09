using SwiftFramework.EditorUtils;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
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
        public static bool IsProcessing { get; private set; }

        private Vector2 scrollPos;

        public void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Separator();

            if (IsProcessing)
            {
                EditorGUILayout.LabelField("Installing...", EditorGUIEx.BoldCenteredLabel);

                if (GUILayout.Button("Cancel"))
                {
                    FinishInstalling(false);
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
                    }

                    plugin.DrawCustomGUI(Repaint, data);
                }

                GUI.color = color;
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdatePlugin(PluginInfo plugin, PluginData data)
        {
            PluginsManifest.Instance.StartProcess(plugin);
            plugin.OnUpdate(data.version, plugin.version);
        }

        private void RemovePlugin(PluginInfo plugin)
        {
            PluginsManifest.Instance.StartProcess(plugin);

            IsProcessing = true;

            Repaint();

            var data = PluginsManifest.Instance.FindData(plugin);

            bool finishOnRecompile = false;

            foreach (var file in data.copiedFiles)
            {
                Debug.LogError($"Path: {file} - { AssetDatabase.DeleteAsset(file)}");
                if (!finishOnRecompile && file.EndsWith(".cs"))
                {
                    Compile.OnFinishedCompile += FinishUninstalling;
                    finishOnRecompile = true;
                }
            }

            data.installed = false;
            data.version = 0;
            data.copiedFiles.Clear();
            
            List<ModuleInstaller> moduleInstallers = GetModuleInstallers(plugin);

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
            IsProcessing = false;

            if (PluginsManifest.Instance.CurrentPlugin == null)
            {
                return;
            }

            PatchModuleManifest();
            
            
            
            PluginsManifest.Instance.FinishProcess();
        }

        private static List<ModuleInstaller> GetModuleInstallers(PluginInfo pluginInfo)
        {
            DirectoryInfo pluginRootDir = pluginInfo.RootDirectory;

            List<ModuleInstaller> moduleInstallers = new List<ModuleInstaller>();

            foreach (string guid in AssetDatabase.FindAssets("t:ModuleInstaller"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains($"/{pluginRootDir.Name}/"))
                {
                    moduleInstallers.Add(AssetDatabase.LoadAssetAtPath<ModuleInstaller>(path));
                }
            }

            return moduleInstallers;
        }

        [MenuItem("SwiftFramework/Plugin Installer/Patch")]
        private static void PatchModuleManifest()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            PluginInfo pluginInfo = PluginsManifest.Instance.CurrentPlugin;
        
            PluginData data = PluginsManifest.Instance.FindData(pluginInfo);

            List<ModuleInstaller> moduleInstallers = GetModuleInstallers(pluginInfo);

            foreach (ModuleInstaller moduleInstaller in moduleInstallers)
            {
                ModuleManifest manifest = CreateInstance<ModuleManifest>();
                manifest.name = moduleInstaller.name;
                var so = new SerializedObject(manifest);
                var moduleProp = so.FindProperty("module");
                moduleProp.FindPropertyRelative("implementationType").stringValue =
                    moduleInstaller.module.implementationType;
                moduleProp.FindPropertyRelative("interfaceType").stringValue =
                    moduleInstaller.module.interfaceType;

                if (moduleInstaller.module.config)
                {
                    moduleProp.FindPropertyRelative("configLink").FindPropertyRelative("Path").stringValue =
                        Folders.Configs + "/" + moduleInstaller.module.config.name;
                    
                    string folder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}/";
                    string configPath = $"{folder}/{moduleInstaller.module.config.name}.asset";
                    Util.EnsureProjectFolderExists(folder);
                    
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(moduleInstaller.module.config), configPath);
                    
                    RegisterFile(configPath);
                }
                
                if (moduleInstaller.module.behaviour)
                {
                    string folder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Behaviours}/";
                    
                    Util.EnsureProjectFolderExists(folder);
                    string behaviourPath = $"{folder}/{moduleInstaller.module.behaviour.name}.prefab";
                    moduleProp.FindPropertyRelative("behaviourLink").FindPropertyRelative("Path").stringValue =
                        Folders.Behaviours + "/" + moduleInstaller.module.behaviour.name;
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(moduleInstaller.module.behaviour), behaviourPath);
                    
                    RegisterFile(behaviourPath);
                }
                
                so.ApplyModifiedProperties();
                string f = ResourcesAssetHelper.RootFolder + "/Modules/";
                Util.EnsureProjectFolderExists(f);
                string manifestPath = ResourcesAssetHelper.RootFolder + "/Modules/" + manifest.ImplementationType.Name +
                                      ".asset";
                AssetDatabase.CreateAsset(manifest, manifestPath);
                RegisterFile(manifestPath);
            }


            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        }
            
        public static void RegisterFile(string localPath)
        {
            var data = PluginsManifest.Instance.CurrentPluginData;
            if (data == null)
            {
                return;
            }
            Debug.LogError(localPath);
            data.copiedFiles.Add(localPath);
            EditorUtility.SetDirty(PluginsManifest.Instance);
        }
        
        private static void FinishUninstalling(bool compiled)
        {
            IsProcessing = false;
            PluginsManifest.Instance.FinishProcess();
        }

        private static void CopyFolders(DirectoryInfo pluginRootDir, PluginData data)
        {
            foreach (DirectoryInfo subDir in pluginRootDir.GetDirectories())
            {
                string targetFolder = subDir.Name;

                if (targetFolder == "ModuleInstallers")
                {
                    continue;
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

        }

        private void Install(PluginInfo plugin)
        {
            PluginsManifest.Instance.StartProcess(plugin);

            IsProcessing = true;

            Repaint();

            PluginData data = pluginsData[plugin];

            data.copiedFiles.Clear();

            if (plugin.CanInstall() == false)
            {
                FinishInstalling(false);
                Repaint();
                return;
            }

            plugin.OnWillInstall();

            SymbolCatalog.Add(plugin.GetSymbols());

            string unityPackage = null;

            DirectoryInfo pluginRootDir = new FileInfo(AssetDatabase.GetAssetPath(plugin)).Directory;

            if (pluginRootDir != null)
            {
                foreach (FileInfo item in pluginRootDir.GetFiles())
                {
                    if (item.Extension == ".unitypackage")
                    {
                        unityPackage = item.FullName;
                    }
                }

                if (unityPackage != null)
                {
                    AssetDatabase.ImportPackage(Util.ToRelativePath(unityPackage), false);
                }

                foreach (PluginInfo.PackageDependency dep in plugin.packageDependencies)
                {
                    Util.AddDependencyToPackageManifest(dep.packageName, dep.version);
                }

                CopyFolders(pluginRootDir, data);
            }


            data.installed = true;

            data.version = plugin.version;
            EditorUtility.SetDirty(PluginsManifest.Instance);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (data.copiedFiles.FindIndex(f => f.EndsWith(".cs")) != -1 
                || plugin.defineSymbols.Length > 0
                || plugin.packageDependencies.Length > 0)
            {
                Compile.OnFinishedCompile += FinishInstalling;
            }
            else
            {
                FinishInstalling(false);
            }
            
            Repaint();
        }

        [MenuItem("SwiftFramework/Plugin Installer/Reset")]
        public static void ResetAll()
        {
            PluginsManifest.Instance.ResetAll();
        }

        [MenuItem("SwiftFramework/Plugin Installer/Install")]
        public static void Install()
        {
            pluginsData.Clear();

            PluginInstaller win = GetWindow<PluginInstaller>(true, "Plugin Installer", true);

            win.MoveToCenter();
        }

    }

}
