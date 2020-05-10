using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(fileName = "BootConfig", menuName = "SwiftFramework/Config/BootConfig")]
    public class BootConfig : ScriptableObject
    {
        public int buildNumber;
        public ModuleManifestLink modulesManifest = Link.Create<ModuleManifestLink>($"Configs/ModuleManifest");
    }
}
