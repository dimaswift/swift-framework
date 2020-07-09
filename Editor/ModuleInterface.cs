using System;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [Serializable]
    public class ModuleInterface
    {
        public ScriptableObject config = null;
        public GameObject behaviour = null;
        public string interfaceType;
        public string implementationType;

        public Type GetInterfaceType() => Type.GetType(interfaceType);

        public Type GetImplementationType() => Type.GetType(implementationType);
    }
}
