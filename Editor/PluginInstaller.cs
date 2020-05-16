using SwiftFramework.EditorUtils;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace SwiftFramework.Core.Editor
{
    [InitializeOnLoad]
    public class PluginInstaller : EditorWindow
    {
        public static string PackageRoot => new DirectoryInfo(Util.GetScriptFolder(typeof(PluginInstaller))).Parent.FullName;

        public static string ResourcesPackagePath => $"{PackageRoot}/InternalResources/";

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
                    FinishInstalling();
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
                var plugin = item.Key;
                var data = item.Value;
                var lineHeight = EditorGUIUtility.singleLineHeight;

                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                GUI.Label(new Rect(rect.x, rect.y, rect.width - INSTALL_BTN_WIDTH, lineHeight), $"{plugin.description} : {plugin.displayVersion}");

                var color = GUI.color;

                if (data.installed)
                {
                    bool canUpdate = data.version != plugin.version && plugin.canBeUpdated;

                    GUI.color = !canUpdate ? EditorGUIEx.GreenColor : EditorGUIEx.YellowColor;

                    var label = canUpdate ? "Update" : "Installed";

                    GUI.enabled = canUpdate;

                    if (GUI.Button(new Rect(rect.x + (rect.width - INSTALL_BTN_WIDTH - DELETE_BTN_WIDTH), rect.y, INSTALL_BTN_WIDTH, lineHeight), label))
                    {
                        UpdatePlugin(plugin, data);
                    }

                    GUI.color = color;

                    GUI.enabled = plugin.CanRemove() && data.copiedFiles.Count > 0;

                    if (GUI.Button(new Rect(rect.x + rect.width - DELETE_BTN_WIDTH, rect.y, DELETE_BTN_WIDTH, lineHeight), "X"))
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

                    if (GUI.Button(new Rect(rect.x + rect.width - INSTALL_BTN_WIDTH - DELETE_BTN_WIDTH, rect.y, INSTALL_BTN_WIDTH + DELETE_BTN_WIDTH, lineHeight), "Install"))
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

            if (!finishOnRecompile)
            {
                FinishInstalling();
            }

            Repaint();
        }

        public static void FinishInstalling()
        {
            IsProcessing = false;

            if (PluginsManifest.Instance.CurrentPlugin == null)
            {
                return;
            }

            PatchModuleManifest();

            PluginsManifest.Instance.FinishProcess();

        }

        [MenuItem("SwiftFramework/Plugin Installer/Patch")]
        private static void PatchModuleManifest()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var manifestGuid = AssetDatabase.FindAssets("t:BaseModuleManifest", new string[] { ResourcesAssetHelper.RootFolder }).FirstOrDefaultFast();

            if (string.IsNullOrEmpty(manifestGuid))
            {
                return;
            }

            var manifest = AssetDatabase.LoadAssetAtPath<BaseModuleManifest>(AssetDatabase.GUIDToAssetPath(manifestGuid));

            if (manifest == null)
            {
                return;
            }

            //PluginInfo pluginInfo = PluginsManifest.Instance.CurrentPlugin;
            PluginInfo pluginInfo = Util.GetAsset<PluginInfo>();

            PluginData data = PluginsManifest.Instance.FindData(pluginInfo);

            DirectoryInfo pluginRootDir = pluginInfo.RootDirectory;

            List<ModuleInstaller> moduleInstallers = new List<ModuleInstaller>();

            foreach (var subDir in pluginRootDir.GetDirectories())
            {
                string targetFolder = subDir.Name;
                if (targetFolder == "ModuleInstallers")
                {
                    foreach (var moduleDir in subDir.GetDirectories())
                    {
                        foreach (var guid in AssetDatabase.FindAssets("t:ModuleInstaller"))
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            if (path.Contains($"/{pluginRootDir.Name}/"))
                            {
                                moduleInstallers.Add(AssetDatabase.LoadAssetAtPath<ModuleInstaller>(path));
                            }
                        }
                    }

                    continue;
                }
            }


            foreach (var moduleInstaller in moduleInstallers)
            {
                var installerDir = AssetDatabase.GetAssetPath(moduleInstaller);

                CopyFolders(new FileInfo(installerDir).Directory, data);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var installerName = moduleInstaller.name + ".asset";

                var root = installerDir.Substring(0, installerDir.Length - installerName.Length - 1);

                var resourcesFolder = "/_Resources/";

                foreach (var link in manifest.GetAllModuleLinks())
                {
                    try
                    {
                        var interfaceType = link.field.CustomAttributes.Where(a => a.AttributeType == typeof(LinkFilterAttribute)).FirstOrDefaultFast()?.ConstructorArguments.FirstOrDefaultFast().Value as Type;

                        if (interfaceType == moduleInstaller.module.GetInterfaceType())
                        {
                            link.link.ImplementationType = moduleInstaller.module.GetImplementationType();

                            foreach (var guid in AssetDatabase.FindAssets("t:Object", new string[] { root }))
                            {
                                var path = AssetDatabase.GUIDToAssetPath(guid);

                                if (string.IsNullOrEmpty(path))
                                {
                                    continue;
                                }

                                var prefab = AssetDatabase.LoadAssetAtPath(path, link.link.ImplementationType);

                                if (prefab)
                                {
                                    var linkPath = path.Substring(root.Length + resourcesFolder.Length, path.Length - root.Length - resourcesFolder.Length - Path.GetExtension(path).Length);
                                    link.link.SetBehaviourPath(linkPath);
                                }
                                var configAttr = link.link.ImplementationType.GetCustomAttribute<ConfigurableAttribute>();
                                if (configAttr != null)
                                {
                                    var config = AssetDatabase.LoadAssetAtPath(path, configAttr.configType);
                                    if (config)
                                    {
                                        var linkPath = path.Substring(root.Length + resourcesFolder.Length, path.Length - root.Length - resourcesFolder.Length - Path.GetExtension(path).Length);
                                        link.link.SetConfigPath(linkPath);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"{link} {e.Message}"); 
                    }
               
                }
            }

            EditorUtility.SetDirty(manifest);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        }

        public static void FinishUninstalling(bool compiled)
        {
            IsProcessing = false;
            PluginsManifest.Instance.FinishProcess();
        }

        private static void CopyFolders(DirectoryInfo pluginRootDir, PluginData data)
        {
            foreach (var subDir in pluginRootDir.GetDirectories())
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

            var data = pluginsData[plugin];

            data.copiedFiles.Clear();

            if (plugin.CanInstall() == false)
            {
                FinishInstalling();
                Repaint();
                return;
            }

            plugin.OnInstall();

            SymbolCatalog.Add(plugin.GetSymbols());

            string unityPackage = null;

            DirectoryInfo pluginRootDir = new FileInfo(AssetDatabase.GetAssetPath(plugin)).Directory;

            foreach (var item in pluginRootDir.GetFiles())
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
            foreach (var dep in plugin.packageDependencies)
            {
                Util.AddDependencyToPackageManifest(dep.packageName, dep.version);
            }

            CopyFolders(pluginRootDir, data);


            data.installed = true;

            data.version = plugin.version;
            EditorUtility.SetDirty(PluginsManifest.Instance);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

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

            var win = GetWindow<PluginInstaller>(true, "Plugin Installer", true);

            win.MoveToCenter();
        }

    }

}
