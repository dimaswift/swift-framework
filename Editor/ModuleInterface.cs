using System;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [Serializable]
    public class ModuleInterface
    {
        public SerializedType configType;
        public SerializedType interfaceType;
        public SerializedType implementationType;
        public GameObject behaviour;
        public ScriptableObject config;
        public bool initializeOnLoad = true;
    }
}
