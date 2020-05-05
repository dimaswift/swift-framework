using System;

namespace SwiftFramework.Core
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
