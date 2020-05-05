using System;

namespace SwiftFramework.Core
{
    public class LinkFilterAttribute : Attribute
    {
        public readonly Type interfaceType;

        public LinkFilterAttribute(Type interfaceType)
        {
            this.interfaceType = interfaceType;
        }
    }
}
