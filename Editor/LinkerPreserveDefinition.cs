using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Linker Preserve Definition")]
    public class LinkerPreserveDefinition : ScriptableObject
    {
        public AssemblyLinkerInfo assemblyToPreserve;
    }
}