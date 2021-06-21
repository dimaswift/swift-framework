using System;

namespace Swift.Core
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
