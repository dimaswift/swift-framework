using SwiftFramework.Editor;
using SwiftFramework.EditorUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class InstallerWindow : EditorWindow
    {
        [NonSerialized] private SerializedObject serializedObject = null;
        [SerializeField] private string nameSpace = "MyGame";
        [SerializeField] private string appName = "Root";
        [SerializeField] private bool useAddressables = true;

        [SerializeField] private List<string> modules = new List<string>();

        [SerializeField] private bool installing = false;
        [SerializeField] private string currentOperation = null;
        [SerializeField] private int operationIndex = 0;


        internal static InstallerWindow Instance => GetWindow<InstallerWindow>();


#if !SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Install")]
#endif
        private static void OpenWindow()
        {
            var win = GetWindow<InstallerWindow>(true, "SwiftFramework Install Wizard", true);
            win.minSize = new Vector2(300, 400);
            win.maxSize = new Vector2(300, 800);

            win.MoveToCenter();
        }

        internal void SetNextInstallOperation(string operation)
        {
            Instance.currentOperation = operation;
            operationIndex++;
            Instance.Repaint();
        }

        private void OnGUI()
        {
            if (installing)
            {
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Installing...");

                EditorGUILayout.Separator();

                EditorGUILayout.LabelField(currentOperation);

                return;
            }

            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(this);
            }

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(nameSpace)));

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(appName)));

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(useAddressables)));

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Modules", EditorGUIEx.BoldCenteredLabel);

            EditorGUILayout.Separator();

            Undo.RecordObject(this, "Installer");

            var modulesProp = serializedObject.FindProperty(nameof(modules));

            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var moduleType = Type.GetType(modulesProp.GetArrayElementAtIndex(i).stringValue);
                var rect = EditorGUILayout.GetControlRect(false);
                var buttonSize = 60;
                var margin = 5;
                rect.width -= buttonSize + margin;

                EditorGUI.LabelField(rect, moduleType.Name, EditorStyles.boldLabel);

                rect.x += rect.width + margin;
                rect.width = buttonSize - margin;

                if (GUI.Button(rect, "Delete"))
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    EditorUtility.SetDirty(this);
                    Repaint();
                }
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Add Module"))
            {
                TypeSelectorWindow.Open(Util.GetAllTypes(IsModuleEligible), "Choose Module").Done(selected =>
                {
                    modulesProp.InsertArrayElementAtIndex(0);
                    modulesProp.GetArrayElementAtIndex(0).stringValue = selected.AssemblyQualifiedName;
                    EditorUtility.SetDirty(this);
                    Repaint();
                });
            }

            EditorGUILayout.Separator();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Install"))
            {
                Install();
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

        private static string GetScriptPath(string scriptName) => $"Assets/Scripts/{scriptName}.cs";

        public void Install()
        {
            operationIndex = 0;

            installing = true;

            SetNextInstallOperation("Generating app...");

            SymbolCatalog.Add("SWIFT_FRAMEWORK_INSTALLED", "SwiftFramework was installed");

            if (useAddressables)
            {
                if (Util.HasPackageDependency("com.unity.addressables") == false)
                {
                    SetNextInstallOperation("Importing addressables package...");

                    Util.AddDependencyToPackageManifest($"com.unity.addressables", Util.AddressableVersion);

                    Compile.OnFinishedCompile += EnableAddressables;
                }
            }
            else
            {
                SymbolCatalog.Disable("USE_ADDRESSABLES");
            }

            Type appClass = Util.FindChildClass(typeof(App));

            if (appClass == null)
            {
                var c = ScriptBuilder.GenerateAppClass(appName, nameSpace);
                ScriptBuilder.SaveClassToDisc(c, GetScriptPath(appName), true, ProcessAppClass);

                Compile.OnFinishedCompile += GenerateAppBoot;
            }
        }

        private static void EnableAddressables(bool compiled)
        {
            SymbolCatalog.Add("USE_ADDRESSABLES", "Enables Unity Addressables");
        }

        private static void GenerateAppBoot(bool compiled)
        {
            Instance.SetNextInstallOperation("Generating boot...");

            var appClass = Util.FindChildClass(typeof(App));
            var bootClass = ScriptBuilder.GenerateAppBootClass(appClass);
            ScriptBuilder.SaveClassToDisc(bootClass, $"Assets/Scripts/AppBoot.cs", true);

            var manifest = ScriptBuilder.GenerateManifestClass(appClass.Namespace, appClass);
            ScriptBuilder.SaveClassToDisc(manifest, GetScriptPath($"ModuleManifest"), true);

            Compile.OnFinishedCompile += GenerateConfigs;
        }

        private static void GenerateConfigs(bool compiled)
        {
            Instance.SetNextInstallOperation("Generating configs...");

            if (!compiled)
            {
                return;
            }

            var configsFolder = ResourcesAssetHelper.RootFolder + "/" + Folders.Configs;

            if (Directory.Exists(configsFolder) == false)
            {
                Directory.CreateDirectory(configsFolder);
            }

            BootConfig bootConfig = Util.CreateScriptable<BootConfig>("BootConfig", configsFolder);

            Type manifestType = Util.FindChildClass(typeof(BaseModuleManifest));

            BaseModuleManifest manifest = Util.CreateScriptable(manifestType, "ModuleManifest", configsFolder) as BaseModuleManifest;

            Instance.Close();
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
    }

    [CustomEditor(typeof(AppBoot), true)]
    internal class BootEditor : UnityEditor.Editor
    {
        private static string BootConfigFolder => ResourcesAssetHelper.RootFolder + "/Configs";

        public static void GenerateModuleManifest()
        {
            Type appClass = Util.FindChildClass(typeof(App));
            Type bootClass = Util.FindChildClass(typeof(AppBoot));
            var manifest = ScriptBuilder.GenerateManifestClass(bootClass.Namespace, appClass);
            ScriptBuilder.SaveClassToDisc(manifest, Path.Combine(Application.dataPath, Util.GetScriptFolder(bootClass), $"ModuleManifest.cs"), false);
        }

        private static T EnsureConfigExists<T>(string configsFolder) where T : ScriptableObject
        {
            T config = Util.FindScriptableObject<T>();
            if (config == null)
            {
                Util.EnsureProjectFolderExists(configsFolder);
                config = Util.CreateScriptable<T>(typeof(T).Name, configsFolder);
            }
            return config;
        }

        private static ScriptableObject EnsureModuleManifestExists()
        {
            Type manifestClass = Util.FindChildClass(typeof(BaseModuleManifest));
            if (manifestClass == null)
            {
                return null;
            }

            ScriptableObject manifest = Util.FindScriptableObject(manifestClass);
            if (manifest == null)
            {
                Debug.Log($"{"Manifest not found"}");
            }
            return manifest;
        }

#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Boot Config")]
#endif
        private static void SelectBootConfig()
        {
            Selection.activeObject = EnsureConfigExists<BootConfig>(BootConfigFolder);
        }

#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Module Manifest")]
#endif
        private static void SelectModuleManifest()
        {
            ScriptableObject manifest = EnsureModuleManifestExists();
            if (manifest == null)
            {
                Debug.LogError("Module Manifest not found!");
                return;
            }
            Selection.activeObject = manifest;
        }


    }
}
