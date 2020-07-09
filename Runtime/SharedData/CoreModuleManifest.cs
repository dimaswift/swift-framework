using UnityEngine;

namespace SwiftFramework.Core
{
    [AddrLabel(AddrLabels.Module)]
    [CreateAssetMenu(menuName = "SwiftFramework/Internal/CoreModuleManifest")]
    internal class CoreModuleManifest : ModuleManifest
    {
        public override string ModuleGroup => ModuleGroups.Core;
    }
}