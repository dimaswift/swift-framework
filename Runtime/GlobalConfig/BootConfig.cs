using System.IO;
using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(fileName = "BootConfig", menuName = "SwiftFramework/Config/BootConfig")]
    public class BootConfig : ScriptableObject
    {
        public int buildNumber;
        public ModuleManifestLink modulesManifest = Link.Create<ModuleManifestLink>($"Configs/ModuleManifest");
        public GlobalConfigLink globalConfig = Link.Create<GlobalConfigLink>($"Configs/GlobalConfig");

        private static BootConfig main;

        public static BootConfig Main
        {
            get
            {
                if(main == null)
                {
                    string path = $"BootConfig";
                    main = Resources.Load<BootConfig>(path);
                    if (main == null)
                    {
                        throw new FileNotFoundException($"BootConfig not found! Should be at Resources/{path}");
                    }
                }
                return main;
            }
        }
    }
}
