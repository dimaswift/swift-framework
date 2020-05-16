using SwiftFramework.EditorUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Internal/Core Plugin Info")]
    public class CorePluginInfo : PluginInfo
    {
        [NonSerialized] private SerializedObject serializedObject = null;
        [SerializeField] private string nameSpace = "MyGame";
        [SerializeField] private string appName = "Root";
        [SerializeField] private bool useAddressables = true;

        [SerializeField] private List<string> modules = new List<string>();

        [SerializeField] private int selectedModule = 0;

        [SerializeField] private bool show;

        [NonSerialized] private string[] moduleNames = new string[0];
        [NonSerialized] private string[] moduleTypes = new string[0];
        [NonSerialized] private List<(string name, string fullName)> moduleNamesBuffer = new List<(string name, string fullName)>();

        private bool allModulesAdded;


        public override void DrawCustomGUI(Action repaintHandler, PluginData data)
        {
            if (!data.installed && PluginInstaller.IsProcessing == false)
            {
                if (Util.FindChildClass(typeof(App)) != null)
                {
                    data.installed = true;
                    return;
                }
            }

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

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(nameSpace)));

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(appName)));

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
                selectedModule = EditorGUI.Popup(new Rect(addModuleRect.x, addModuleRect.y, addModuleRect.width - 50, addModuleRect.height), selectedModule, moduleNames);

                if (GUI.Button(new Rect(addModuleRect.x + addModuleRect.width - 50, addModuleRect.y, 50, addModuleRect.height), "Add"))
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
                var moduleType = Type.GetType(modulesProp.GetArrayElementAtIndex(i).stringValue);
                if (moduleType == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    continue;
                }
                var rect = EditorGUILayout.GetControlRect(false);
                var buttonSize = 50;

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



        private static string GetScriptPath(string scriptName) => $"Assets/Scripts/{scriptName}.cs";

        public override bool CanInstall()
        {
            Type appClass = Util.FindChildClass(typeof(App));
            return appClass == null;
        }

        public override bool CanRemove()
        {
            return true;
        }

        public override void OnInstall()
        {
            EditorUtility.DisplayProgressBar("Installing", "Generating app script...", .1f);

            Type appClass = Util.FindChildClass(typeof(App));

            SymbolCatalog.Add("SWIFT_FRAMEWORK_INSTALLED", "SwiftFramework was installed");

            var c = ScriptBuilder.GenerateAppClass(appName, nameSpace);
            ScriptBuilder.SaveClassToDisc(c, GetScriptPath(appName), true, ProcessAppClass);
            RegisterFile(GetScriptPath(appName));
            Compile.OnFinishedCompile += GenerateAppBoot;

            if (useAddressables)
            {
                if (Util.HasPackageDependency("com.unity.addressables") == false)
                {
                    EditorUtility.DisplayProgressBar("Installing", "Importing addressables package...", .2f);
                    Util.AddDependencyToPackageManifest($"com.unity.addressables", Util.AddressableVersion);

                    Compile.OnFinishedCompile += EnableAddressables;
                }
            }
            else
            {
                SymbolCatalog.Disable("USE_ADDRESSABLES");
            }
        }

        private static void EnableAddressables(bool compiled)
        {
            EditorUtility.DisplayProgressBar("Installing", "Enabling addressables...", .3f);
            SymbolCatalog.Add("USE_ADDRESSABLES", "Enables Unity Addressables");
        }

        private static void GenerateAppBoot(bool compiled)
        {
            EditorUtility.DisplayProgressBar("Installing", "Generating boot...", .5f);

            var appClass = Util.FindChildClass(typeof(App));
            var bootClass = ScriptBuilder.GenerateAppBootClass(appClass);
            var bootPath = $"Assets/Scripts/AppBoot.cs";
            ScriptBuilder.SaveClassToDisc(bootClass, bootPath, true);

            var data = PluginsManifest.Instance.CurrentPluginData;

            data.copiedFiles.Add(bootPath);

            var manifest = ScriptBuilder.GenerateManifestClass(appClass.Namespace, appClass);
            var manifestPath = GetScriptPath($"ModuleManifest");
            ScriptBuilder.SaveClassToDisc(manifest, manifestPath, true);

            RegisterFile(manifestPath);

            Compile.OnFinishedCompile += GenerateConfigs;
        }


        private static void RegisterFile(string localPath)
        {
            var data = PluginsManifest.Instance.CurrentPluginData;
            if (data == null)
            {
                return;
            }
            data.copiedFiles.Add(localPath);
            EditorUtility.SetDirty(PluginsManifest.Instance);
        }

        private static void GenerateConfigs(bool compiled)
        {
            EditorUtility.DisplayProgressBar("Installing", "Generating configs...", .7f);

            if (!compiled)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            var configsFolder = ResourcesAssetHelper.RootFolder + "/" + Folders.Configs;

            if (Directory.Exists(configsFolder) == false)
            {
                Directory.CreateDirectory(configsFolder);
            }

            BootConfig bootConfig = Util.CreateScriptable<BootConfig>("BootConfig", configsFolder);

            RegisterFile(AssetDatabase.GetAssetPath(bootConfig));

            Type manifestType = Util.FindChildClass(typeof(BaseModuleManifest));

            BaseModuleManifest manifest = Util.CreateScriptable(manifestType, "ModuleManifest", configsFolder) as BaseModuleManifest;

            RegisterFile(AssetDatabase.GetAssetPath(manifest));

            EditorUtility.ClearProgressBar();

            PluginInstaller.FinishInstalling();
        }

        private void ProcessAppClass(List<string> scriptLines)
        {
            var firstLine = scriptLines.FindIndex(l => l.Contains("public class")) + 2;

            int i = 0;

            foreach (var module in modules)
            {
                if (i > 0)
                {
                    scriptLines.Insert(firstLine, $"");
                }

                var moduleType = Type.GetType(module);
                var moduleName = moduleType.Name;
                var fieldName = moduleName.Remove(0, 1);

                fieldName = fieldName.ToString().ToLower()[0] + fieldName.Substring(1, fieldName.Length - 1);

                scriptLines.Insert(firstLine, $"        public {moduleName} {moduleName.Remove(0, 1)} => GetCachedModule(ref {fieldName});");
                scriptLines.Insert(firstLine, $"        private {moduleName} {fieldName};");

                i++;
            }

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

            var group = type.GetCustomAttribute<ModuleGroupAttribute>();

            if (group != null && group.GroupId == ModuleGroups.Core)
            {
                return false;
            }

            if (modules.Find(m => Type.GetType(m) == type) != null)
            {
                return false;
            }

            return true;

        }

    }
}
