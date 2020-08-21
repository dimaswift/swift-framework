using System;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Module Install Info")]
    public class ModuleInstallInfo : ScriptableObject
    {
        [SerializeField] private ModuleInterface module = null;

        public ModuleLink GenerateLink()
        {
            ModuleLink link = new ModuleLink()
            {
                InterfaceType = module.interfaceType.Type,
                InitializeOnLoad = module.initializeOnLoad
            };
            
            link.SetImplementation(module.implementationType.TypeString);

            if (module.configType.IsDefined)
            {
                string path = Folders.Configs + "/" + module.configType.Name;
                link.ConfigLink = Link.Create<ModuleConfigLink>(path);

                path = $"{ResourcesAssetHelper.RootFolder}/{path}.asset";

                if (AssetDatabase.LoadAssetAtPath<ModuleConfig>(path) == null)
                {
                    Util.EnsureProjectFolderExists($"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}");
                    
                    if (module.config)
                    {
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(module.config), path);
                    }
                    else
                    {
                        Object config = CreateInstance(module.configType.Type);
                        config.name = module.configType.Name;
                        AssetDatabase.CreateAsset(config, path);
                    }
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
                }
            }

            return link;
        }

        public Type GetInterfaceType()
        {
            return module.interfaceType.Type;
        }

        public string GetModuleDescription()
        {
            if (string.IsNullOrEmpty(module.implementationType.TypeString))
            {
                return "Invalid type";
            }

            string[] values = module.implementationType.TypeString.Split(',');

            if (values.Length == 0)
            {
                return "Invalid type";
            }
            
            return values[0];
        }

        public Type GetImplementationType()
        {
            return module.implementationType.Type;
        }
    }
}
