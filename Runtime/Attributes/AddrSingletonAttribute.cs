using System;

namespace Swift.Core
{
    public class AddrSingletonAttribute : Attribute
    {
        public readonly string folder;

        public AddrSingletonAttribute(string folder = null)
        {
            this.folder = folder;
        }
    }
}
