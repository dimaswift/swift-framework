using System;
using UnityEditorInternal;

namespace SwiftFramework.Core.Editor
{
    [Serializable]
    public class AssemblyLinkerInfo
    {
        public AssemblyDefinitionAsset assembly;
        public string[] rootNamespaces = {};
    }
}