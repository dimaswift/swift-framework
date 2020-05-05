using SwiftFramework.EditorUtils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomEditor(typeof(AppBoot), true)]
    internal class BootEditor : UnityEditor.Editor
    {
        private const string configsFolder = "Assets/Configs";
        private const string bootConfigFolder = "Assets/Configs/Resources";

        [MenuItem("SwiftFramework/Generate Configs")]
        public static void GenerateConfigs()
        {
            EnsureConfigExists<BootConfig>(bootConfigFolder);
            EnsureConfigExists<GlobalConfig>(configsFolder);

            EnsureModuleManifestExists();

            GenerateModuleManifest();
        } 

        public static void GenerateModuleManifest()
        {
            Type bootClass = Util.FindChildClass(typeof(AppBoot));
            Type gameClass = Util.FindChildClass(typeof(App));
            if (bootClass != null && gameClass != null)
            {
                var manifest = Util.GenerateManifestClass(bootClass.Namespace, gameClass);
                Util.SaveClassToDisc(manifest, Path.Combine(Application.dataPath, Util.GetScriptFolder(bootClass), $"ModuleManifest.cs"), false);
            }
            EnsureModuleManifestExists();
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
            if(manifestClass == null)
            {
                return null;
            }
            ScriptableObject manifest = Util.FindScriptableObject(manifestClass);
            if(manifest == null)
            {
                Util.EnsureProjectFolderExists(configsFolder);
                manifest = Util.CreateScriptable(manifestClass, "ModuleManifest", configsFolder) as ScriptableObject;
            }
            return manifest;
        }

       
        [MenuItem("SwiftFramework/Global Config")]
        private static void SelectGlobalConfig()
        {
            Selection.activeObject = EnsureConfigExists<GlobalConfig>(configsFolder);
        }

        [MenuItem("SwiftFramework/Boot Config")]
        private static void SelectBootConfig()
        {
            Selection.activeObject = EnsureConfigExists<BootConfig>(bootConfigFolder);
        }

        [MenuItem("SwiftFramework/Module Manifest")]
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
