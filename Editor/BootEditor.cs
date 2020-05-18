using System;
using System.CodeDom;
using System.IO;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomEditor(typeof(AppBoot), true)]
    internal class BootEditor : UnityEditor.Editor
    {
        private static string BootConfigFolder => ResourcesAssetHelper.RootFolder + "/Configs";

        public static void GenerateModuleManifest()
        {
            Type appClass = Util.FindChildClass(typeof(App));
            Type bootClass = Util.FindChildClass(typeof(AppBoot));
            CodeCompileUnit manifest = ScriptBuilder.GenerateManifestClass(bootClass.Namespace, appClass);
            ScriptBuilder.SaveClassToDisc(manifest,
                Path.Combine(Application.dataPath, Util.GetScriptFolder(bootClass), $"ModuleManifest.cs"), false);
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
                Debug.Log("Manifest not found");
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