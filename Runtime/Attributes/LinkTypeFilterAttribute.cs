using System;

namespace SwiftFramework.Core
{
    public class LinkTypeFilterAttribute : Attribute
    {
        public readonly Type type;

        public LinkTypeFilterAttribute(Type type)
        {
            this.type = type;
        }
    }


}
