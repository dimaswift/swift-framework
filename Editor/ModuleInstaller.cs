using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Internal/Module Installer")]
    public class ModuleInstaller : ScriptableObject
    {
        public ModuleInterface module;
    }
}
