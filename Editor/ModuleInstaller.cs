using System;
using System.Collections.Generic;
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
            AssetsUtil.OnAssetsPostProcessed += AssetsUtilOnOnAssetsPostProcessed;
        }

        private void OnDisable()
        {
            AssetsUtil.OnAssetsPostProcessed -= AssetsUtilOnOnAssetsPostProcessed;
        }

        public void OnGUI()
        {
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
            
            if (modulesAreUpToDate == false || modules.Count == 0)
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
                string moduleName = moduleInterface.Name.Remove(0, 1);
                string label = System.Text.RegularExpressions.Regex.Replace(moduleName, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
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
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(module.manifest));
                    }
                }
            }
        }
        
        public static ModuleManifest Install(Type moduleInterface)
        {
            ModuleGroupAttribute groupAttribute = moduleInterface.GetCustomAttribute<ModuleGroupAttribute>();
            string moduleName = moduleInterface.Name.Remove(0, 1);
            ModuleManifest manifest = CreateInstance<ModuleManifest>();
            manifest.name = moduleName;
            Type implementation = Util.FindImplementation(moduleInterface);
            manifest.Link = new ModuleLink()
            {
                InterfaceType = moduleInterface,
                ImplementationType = implementation,
                ConfigLink = LoadOrCreateConfig(implementation),
                BehaviourLink = LoadOrCreateBehavior(implementation)
            };
            AssetDatabase.CreateAsset(manifest, $"{ResourcesAssetHelper.RootFolder}/{Folders.Modules}/{manifest.name}.asset");
            return manifest;
        }

        private static BehaviourModuleLink LoadOrCreateBehavior(Type implementation)
        {
            if (implementation == null)
            {
                return Link.CreateNull<BehaviourModuleLink>();
            }

            if (typeof(BehaviourModule).IsAssignableFrom(implementation) == false)
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
            }

            return ResourcesAssetHelper.CreateLink<BehaviourModuleLink>(asset);
        }

        private static ModuleConfigLink LoadOrCreateConfig(Type implementation)
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
                asset = CreateInstance(configurableAttribute.configType.Name);
                asset.name = configurableAttribute.configType.Name;
                string configPath = ResourcesAssetHelper.RootFolder + "/Configs/" + asset.name + ".asset";
                AssetDatabase.CreateAsset(asset, configPath);
            }

            return ResourcesAssetHelper.CreateLink<ModuleConfigLink>(asset);
        }

        private void AssetsUtilOnOnAssetsPostProcessed()
        {
            modulesAreUpToDate = false;
        }

        [MenuItem("SwiftFramework/Modules")]
        public static void OpenWindow()
        {
            ModuleInstaller win = GetWindow<ModuleInstaller>(true, "Modules", true);
            win.minSize = new Vector2(400, win.minSize.y);
            win.MoveToCenter();
        }
    }
}