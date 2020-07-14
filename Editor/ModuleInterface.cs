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

        private Type cachedInterfaceType;
        private Type cachedImplementationType;


        public Type GetInterfaceType()
        {
            if (cachedInterfaceType != null)
            {
                return cachedInterfaceType;
            }

            cachedInterfaceType = Type.GetType(interfaceType);
            return cachedInterfaceType;
        }

        public void ResetCache()
        {
            cachedImplementationType = null;
            cachedInterfaceType = null;
        }

        public Type GetImplementationType()
        {
            if (cachedImplementationType != null)
            {
                return cachedImplementationType;
            }

            cachedImplementationType = Type.GetType(implementationType);
            return cachedImplementationType;
        }
    }
}
