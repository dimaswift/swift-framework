using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Internal/Module Install Info")]
    public class ModuleInstallInfo : ScriptableObject
    {
        public bool isCoreModule;
        public ModuleInterface module;
    }
}
