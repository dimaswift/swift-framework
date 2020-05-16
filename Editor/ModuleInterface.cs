using System;

namespace SwiftFramework.Core.Editor
{
    [Serializable]
    public class ModuleInterface
    {
        public string interfaceType;
        public string implementationType;

        public Type GetInterfaceType() => Type.GetType(interfaceType);

        public Type GetImplementationType() => Type.GetType(implementationType);
    }
}
