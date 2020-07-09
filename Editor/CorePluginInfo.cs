using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Internal/Core Plugin Info")]
    public class CorePluginInfo : PluginInfo
    {
        [NonSerialized] private SerializedObject serializedObject = null;

        [SerializeField] private bool useAddressables = true;

        [SerializeField] private List<string> modules = new List<string>();

        [SerializeField] private int selectedModule = 0;

        [SerializeField] private bool show;

        [NonSerialized] private string[] moduleNames = new string[0];
        [NonSerialized] private string[] moduleTypes = new string[0];

        [NonSerialized]
        private readonly List<(string name, string fullName)> moduleNamesBuffer = new List<(string name, string fullName)>();

        private bool allModulesAdded;


        public override void DrawCustomGUI(Action repaintHandler, PluginData data)
        {
            show = EditorGUILayout.BeginFoldoutHeaderGroup(show, "Options");

            EditorGUI.indentLevel++;

            if (show == false)
            {
                return;
            }

            if (serializedObject == null)
            {
                allModulesAdded = false;
                serializedObject = new SerializedObject(this);
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(useAddressables)));

            EditorGUILayout.LabelField("Modules", EditorGUIEx.BoldCenteredLabel);

            Undo.RecordObject(this, "Installer");

            var modulesProp = serializedObject.FindProperty(nameof(modules));

            if (allModulesAdded == false && moduleNames.Length == 0)
            {
                var modulesTypes = Util.GetAllTypes(IsModuleEligible);

                moduleNamesBuffer.Clear();

                foreach (var type in modulesTypes)
                {
                    if (modules.Contains(type.AssemblyQualifiedName))
                    {
                        continue;
                    }

                    moduleNamesBuffer.Add((type.Name, type.AssemblyQualifiedName));
                }

                moduleNames = new string[moduleNamesBuffer.Count];
                moduleTypes = new string[moduleNamesBuffer.Count];

                for (int i = 0; i < moduleNamesBuffer.Count; i++)
                {
                    moduleNames[i] = moduleNamesBuffer[i].name;
                    moduleTypes[i] = moduleNamesBuffer[i].fullName;
                }

                selectedModule = 0;

                allModulesAdded = moduleNamesBuffer.Count == 0;
            }

            Rect addModuleRect = EditorGUILayout.GetControlRect();

            if (moduleNames.Length > 0)
            {
                selectedModule =
                    EditorGUI.Popup(
                        new Rect(addModuleRect.x, addModuleRect.y, addModuleRect.width - 50, addModuleRect.height),
                        selectedModule, moduleNames);

                if (GUI.Button(
                    new Rect(addModuleRect.x + addModuleRect.width - 50, addModuleRect.y, 50, addModuleRect.height),
                    "Add"))
                {
                    modulesProp.InsertArrayElementAtIndex(0);
                    modulesProp.GetArrayElementAtIndex(0).stringValue = moduleTypes[selectedModule];
                    EditorUtility.SetDirty(this);
                    allModulesAdded = false;
                    moduleNames = new string[0];
                    repaintHandler();
                }
            }
            else
            {
                EditorGUI.LabelField(addModuleRect, "All modules selected", EditorStyles.centeredGreyMiniLabel);
            }

            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                Type moduleType = Type.GetType(modulesProp.GetArrayElementAtIndex(i).stringValue);
                if (moduleType == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    continue;
                }

                Rect rect = EditorGUILayout.GetControlRect(false);
                const int buttonSize = 50;

                rect.width -= buttonSize;

                EditorGUI.LabelField(rect, moduleType.Name, EditorStyles.boldLabel);

                rect.x += rect.width;
                rect.width = buttonSize;

                if (GUI.Button(rect, "Delete"))
                {
                    var moduleToDelete = modulesProp.GetArrayElementAtIndex(i).stringValue;
                    modulesProp.DeleteArrayElementAtIndex(i);
                    EditorUtility.SetDirty(this);
                    allModulesAdded = false;
                    moduleNames = new string[0];
                    modules.RemoveAll(m => m == moduleToDelete);
                    repaintHandler();
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Separator();


            EditorGUI.indentLevel--;
        }
        
        public override bool CanInstall()
        {
            return Util.GetAssets<BootConfig>().CountFast() == 0;
        }

        public override bool CanRemove()
        {
            return true;
        }

        public override void OnWillInstall()
        {
            SymbolCatalog.Add("SWIFT_FRAMEWORK_INSTALLED", "SwiftFramework was installed");
            
            if (useAddressables)
            {
                if (Util.HasPackageDependency("com.unity.addressables") == false)
                {
                    Debug.LogError("wtf");
                    Util.AddDependencyToPackageManifest($"com.unity.addressables", Util.ADDRESSABLE_VERSION);

                    Compile.OnFinishedCompile += EnableAddressables;
                }
                else
                {
                    GenerateConfigs(true);
                }
            }
            else
            {
                SymbolCatalog.Disable("USE_ADDRESSABLES");
            }
        }

        private static void EnableAddressables(bool compiled)
        {
            SymbolCatalog.Add("USE_ADDRESSABLES", "Enables Unity Addressables");
            Compile.OnFinishedCompile += GenerateConfigs;
        }

        private static void GenerateConfigs(bool compiled)
        {
            var configsFolder = ResourcesAssetHelper.RootFolder + "/" + Folders.Configs;

            if (Directory.Exists(configsFolder) == false)
            {
                Directory.CreateDirectory(configsFolder);
            }

            BootConfig bootConfig = Util.CreateScriptable<BootConfig>("BootConfig", configsFolder);

            PluginInstaller.RegisterFile(AssetDatabase.GetAssetPath(bootConfig));
        }
        
        private bool IsModuleEligible(Type type)
        {
            if (type.IsInterface == false || type.IsVisible == false || type == typeof(IModule))
            {
                return false;
            }

            if (typeof(IModule).IsAssignableFrom(type) == false)
            {
                return false;
            }

            ModuleGroupAttribute group = type.GetCustomAttribute<ModuleGroupAttribute>();

            if (group != null && group.GroupId == ModuleGroups.Core)
            {
                return false;
            }

            return modules.Find(m => Type.GetType(m) == type) == null;
        }
    }
}