using System;

namespace Swift.Core
{
    public class AddrGroupAttribute : Attribute
    {
        public readonly string groupName;

        public AddrGroupAttribute(string groupName)
        {
            this.groupName = groupName;
        }
    }
}
