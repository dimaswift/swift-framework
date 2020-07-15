using System;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Module Install Info")]
    public class ModuleInstallInfo : ScriptableObject
    {
        [SerializeField] private ModuleInterface module = null;

        public ModuleLink GenerateLink(Action<string> onAssetCreated)
        {
            ModuleLink link = new ModuleLink()
            {
                InterfaceType = module.GetInterfaceType(),
            };
            
            link.SetImplementation(module.implementationType);

            if (module.config != null)
            {
                string path = Folders.Configs + "/" + module.config.name;
                link.ConfigLink = Link.Create<ModuleConfigLink>(path);

                path = $"{ResourcesAssetHelper.RootFolder}/{path}.asset";

                if (AssetDatabase.LoadAssetAtPath<ModuleConfig>(path) == null)
                {
                    Util.EnsureProjectFolderExists($"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}");
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(module.config), path);
                    onAssetCreated(path);
                }
            }

            if (module.behaviour != null)
            {
                string path = Folders.Behaviours + "/" + module.behaviour.name; 
                link.BehaviourLink = Link.Create<BehaviourModuleLink>(path);

                path = $"{ResourcesAssetHelper.RootFolder}/{path}.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Util.EnsureProjectFolderExists($"{ResourcesAssetHelper.RootFolder}/{Folders.Behaviours}");
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(module.behaviour), path);
                    onAssetCreated(path);
                }
            }
            
            return link;
        }

        public Type GetInterfaceType()
        {
            return module.GetInterfaceType();
        }

        public string GetModuleDescription()
        {
            if (string.IsNullOrEmpty(module.implementationType))
            {
                return "Invalid type";
            }

            string[] values = module.implementationType.Split(',');

            if (values.Length == 0)
            {
                return "Invalid type";
            }
            
            return values[0];
        }

        public Type GetImplementationType()
        {
            return module.GetImplementationType();
        }
    }
}
