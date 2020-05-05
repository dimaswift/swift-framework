using System;

namespace SwiftFramework.Core
{
    public class AddrLabelAttribute : Attribute
    {
        public readonly string[] labels;

        public AddrLabelAttribute(params string[] labels)
        {
            this.labels = labels;
        }
    }
}
