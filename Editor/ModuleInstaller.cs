using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core.Editor
{
    public class ModuleInstaller : EditorWindow
    {
        private Vector2 scrollPos;
        
        private readonly List<(ModuleManifest manifest, SerializedObject serializedObject)> modules 
            = new List<(ModuleManifest manifest, SerializedObject serializedObject)>();
        
        private readonly Dictionary<string, List<Type>> interfacesBuffer = new Dictionary<string, List<Type>>();

        private bool modulesAreUpToDate;

        private bool ShowCoreModules
        {
            get => EditorPrefs.GetBool(nameof(ShowCoreModules));
            set => EditorPrefs.SetBool(nameof(ShowCoreModules), value);
        }
        
        private bool ShowCustomModules
        {
            get => EditorPrefs.GetBool(nameof(ShowCustomModules));
            set => EditorPrefs.SetBool(nameof(ShowCustomModules), value);
        }
        
        private void OnEnable()
        {
            modulesAreUpToDate = false;
            AssetsUtil.OnAssetsPostProcessed += AssetsUtilOnOnAssetsPostProcessed;
        }

        private void OnDisable()
        {
            AssetsUtil.OnAssetsPostProcessed -= AssetsUtilOnOnAssetsPostProcessed;
        }

        public void OnGUI()
        {
            this.ShowCompileAndPlayModeWarning(out bool canEdit);
            
            if (canEdit == false)
            {
                return;
            }
            
            bool coreInstalled = false;
            
#if SWIFT_FRAMEWORK_INSTALLED
            coreInstalled = true;
#endif

            if (coreInstalled == false)
            {
                EditorGUILayout.LabelField("SwiftFramework Core not installed!", EditorGUIEx.BoldCenteredLabel);

                if (GUILayout.Button("Install"))
                {
                    PluginInstaller.OpenWindow();
                    Close();
                }
                
                return;
            }
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Modules", EditorGUIEx.BoldCenteredLabel);
            
            EditorGUILayout.Separator();
            
            ShowCustomModules = EditorGUILayout.BeginFoldoutHeaderGroup(ShowCustomModules, "Custom");
            
            if (ShowCustomModules)
            {
                DrawModules(ModuleGroups.Custom, Util.GetCustomModuleInterfaces);
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            ShowCoreModules = EditorGUILayout.BeginFoldoutHeaderGroup(ShowCoreModules, "Core");
            
            if (ShowCoreModules)
            {
                DrawModules(ModuleGroups.Core, () => Util.GetModuleInterfaces(ModuleGroups.Core));
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Separator();
            
            EditorGUILayout.EndScrollView();
            
            EditorUtility.SetDirty(this);
        }

        private void DrawModules(string groupId, Func<IEnumerable<Type>> typesHandler)
        {
            if (interfacesBuffer.TryGetValue(groupId, out List<Type> types) == false)
            {
                types = new List<Type>(typesHandler());
                
                interfacesBuffer.Add(groupId, types);
                 
                List<Type> definedModules = new List<Type>();

                foreach (Type type in types)
                {
                    var mod = modules.Find(m => m.manifest.InterfaceType == type);
                    if (mod.manifest != null)
                    {
                        definedModules.Add(type);
                    }
                }

                types.RemoveAll(m => definedModules.Contains(m));
                types.InsertRange(0, definedModules);
            }
            
            if (modulesAreUpToDate == false)
            {
                modulesAreUpToDate = true;
                modules.Clear();
                foreach (ModuleManifest manifest in Util.GetAssets<ModuleManifest>())
                {
                    modules.Add((manifest, new SerializedObject(manifest)));
                }
                interfacesBuffer.Clear();
            }

            
            foreach (Type moduleInterface in types)
            {
                string label = moduleInterface.GetDisplayName();
                (ModuleManifest manifest, SerializedObject serializedObject) module = modules.Find(m => m.manifest.InterfaceType == moduleInterface);
                if (module.manifest == null)
                {
                    Rect pos = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    GUI.Label(new Rect(pos.x, pos.y, pos.width / 2, pos.height), label);
                    if (GUI.Button(new Rect(pos.x + pos.width - 50, pos.y, 50, pos.height),  "Install"))
                    {
                        Install(moduleInterface);
                        modulesAreUpToDate = false;
                    }
                }
                else
                {
                    float spacing = EditorGUIUtility.standardVerticalSpacing;
                    Rect labelPos = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + spacing * 2);
                    labelPos.x += spacing * 2;
                    labelPos.y += spacing;
                    SerializedProperty property = module.serializedObject.FindProperty("module");
                    Rect pos = EditorGUILayout.GetControlRect(false, ModuleLinkDrawer.GetPropertyHeight(EditorGUIUtility.singleLineHeight, property) + spacing * 4);

                    GUI.Label(
                        new Rect(pos.x, labelPos.y, pos.width, pos.height + labelPos.height),
                        "", EditorStyles.helpBox);
                    
                    GUI.Label(labelPos, label, EditorStyles.boldLabel);
                    
                    pos.width -= spacing * 4;
                    pos.x += spacing * 2;
                    pos.y += spacing;
                    pos.height -= spacing * 4;
                    
                    ModuleLinkDrawer.Draw(pos, property, new GUIContent(label), false, false);

                    if (GUI.Button(new Rect(labelPos.x + labelPos.width - 50 - spacing * 4, labelPos.y + spacing * 2, 50, labelPos.height - spacing * 2),
                        "Delete"))
                    {
                        if (EditorUtility.DisplayDialog("Warning", $"Do you want to delete {label} module?", "Yes", "Cancel"))
                        {
                            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(module.manifest));
                        }
                    }

                    Color color = GUI.color;
                    string toggleLabel = module.manifest.State == ModuleState.Enabled ? "Disable" : "Enable";
                    GUI.color = module.manifest.State == ModuleState.Enabled
                        ? EditorGUIEx.GreenColor
                        : EditorGUIEx.WarningRedColor;
                    
                    if (GUI.Button(new Rect(labelPos.x + labelPos.width - 110 - spacing * 4, labelPos.y + spacing * 2, 60, labelPos.height - spacing * 2),
                        toggleLabel))
                    {
                        Undo.RecordObject(module.manifest, "Module Manifest");
                        module.manifest.State = module.manifest.State == ModuleState.Enabled
                            ? ModuleState.Disabled
                            : ModuleState.Enabled;
                        EditorUtility.SetDirty(module.manifest);
                        AssetsUtil.TriggerPostprocessEvent();
                    }
                    
                    GUI.color = color;
                }
            }
            Repaint();
        }
        
        public static ModuleManifest Install(Type moduleInterface, Action<string> onAssetCreated = null)
        {
            Type implementation = Util.FindImplementation(moduleInterface);
            ModuleLink link = new ModuleLink()
            {
                InterfaceType = moduleInterface,
                ImplementationType = implementation,
                ConfigLink = LoadOrCreateConfig(implementation),
                BehaviourLink = LoadOrCreateBehavior(implementation)
            };
            return Install(link, onAssetCreated);
        }
        
        public static ModuleManifest Install(ModuleLink link,  Action<string> onAssetCreated = null)
        {
            if (link.InterfaceType == null)
            {
                Debug.LogWarning($"Cannot install module from link: {link}. Interface type not found!");
                return null;
            }

            ModuleManifest manifest = Util.GetAssets<ModuleManifest>()
                .Where(m => m.InterfaceType == link.InterfaceType).FirstOrDefaultFast();

            if (manifest == null)
            {
                manifest = CreateInstance<ModuleManifest>();
                string moduleName = link.InterfaceType.Name.Remove(0, 1);
                manifest.name = moduleName;
                string folder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Modules}";
                string path = $"{folder}/{manifest.name}.asset";
                Util.EnsureProjectFolderExists(folder);
                AssetDatabase.CreateAsset(manifest, path);
                onAssetCreated?.Invoke(path);
            }

            manifest.State = ModuleState.Enabled;
            
            manifest.Link = link.DeepCopy();
            
            EditorUtility.SetDirty(manifest);
            
            AssetsUtil.TriggerPostprocessEvent();
            
            return manifest;
        }

        private static BehaviourModuleLink LoadOrCreateBehavior(Type implementation, Action<string> onAssetCreated = null)
        {
            if (implementation == null)
            {
                return Link.CreateNull<BehaviourModuleLink>();
            }

            if (typeof(BehaviourModule).IsAssignableFrom(implementation) == false)
            {
                return Link.CreateNull<BehaviourModuleLink>();
            }

            DisallowCustomModuleBehavioursAttribute disallowCustomModuleBehavioursAttribute
                = implementation.GetCustomAttribute<DisallowCustomModuleBehavioursAttribute>();

            if (disallowCustomModuleBehavioursAttribute != null)
            {
                return Link.CreateNull<BehaviourModuleLink>();
            }
            
            Object asset = Util.GetAssets(implementation, "", ResourcesAssetHelper.RootFolder).FirstOrDefaultFast();

            if (asset == null)
            {
                GameObject behavior = new GameObject(implementation.Name);
                behavior.AddComponent(implementation); 
                string prefabPath = ResourcesAssetHelper.RootFolder + "/Behaviours/" + behavior.name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(behavior, prefabPath);
                DestroyImmediate(behavior);
                asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                onAssetCreated?.Invoke(prefabPath);
            }

            return ResourcesAssetHelper.CreateLink<BehaviourModuleLink>(asset);
        }

        private static ModuleConfigLink LoadOrCreateConfig(Type implementation, Action<string> onAssetCreated = null)
        {
            if (implementation == null)
            {
                return Link.CreateNull<ModuleConfigLink>();
            }

            ConfigurableAttribute configurableAttribute = implementation.GetCustomAttribute<ConfigurableAttribute>();

            if (configurableAttribute == null)
            {
                return Link.CreateNull<ModuleConfigLink>(); 
            }
            
            Object asset = Util.GetAssets(configurableAttribute.configType, "", ResourcesAssetHelper.RootFolder).FirstOrDefaultFast();

            if (asset == null)
            {
                asset = CreateInstance(configurableAttribute.configType);
                asset.name = configurableAttribute.configType.Name;
                string configPath = ResourcesAssetHelper.RootFolder + "/Configs/" + asset.name + ".asset";
                Util.EnsureProjectFolderExists(ResourcesAssetHelper.RootFolder + "/Configs/");
                onAssetCreated?.Invoke(configPath);
                AssetDatabase.CreateAsset(asset, configPath);
            }

            return ResourcesAssetHelper.CreateLink<ModuleConfigLink>(asset);
        }

        private void AssetsUtilOnOnAssetsPostProcessed()
        {
            modulesAreUpToDate = false;
            Repaint();
        }

        [MenuItem("SwiftFramework/Modules")]
        public static void OpenWindow()
        {
            ModuleInstaller win = GetWindow<ModuleInstaller>("Modules", true);
            win.minSize = new Vector2(400, win.minSize.y);
        }

        public static bool IsModuleInstallationValid(ModuleInstallInfo moduleInfo)
        {
            ModuleManifest manifest = Util.GetAssets<ModuleManifest>("", ResourcesAssetHelper.RootFolder).FirstOrDefault(
                m => m.InterfaceType == moduleInfo.GetInterfaceType());

            if (manifest == null)
            {
                return false;
            }
            
            if (manifest.State == ModuleState.Disabled)
            {
                return false;
            }

            if (manifest.Link.HasInterface == false || manifest.Link.HasImplementation == false)
            {
                return false;
            }
            
            if (manifest.ImplementationType.GetCustomAttribute<ConfigurableAttribute>() != null)
            {
                if (manifest.Link.ConfigLink == null || manifest.Link.ConfigLink.Value() == null)
                {
                    return false;
                }
            }
            
            if (typeof(BehaviourModule).IsAssignableFrom(manifest.ImplementationType))
            {
                if (manifest.ImplementationType.GetCustomAttribute<DisallowCustomModuleBehavioursAttribute>() == null)
                {
                    if (manifest.Link != null && manifest.Link.BehaviourLink != null && !manifest.Link.BehaviourLink.IsEmpty)
                    {
                        string prefabPath = ResourcesAssetHelper.RootFolder + "/" + manifest.Link.BehaviourLink.GetPath() +
                                            ".prefab";
                        if (manifest.Link.BehaviourLink == null ||
                            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}