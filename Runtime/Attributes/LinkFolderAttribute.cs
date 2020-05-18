using System;

namespace SwiftFramework.Core
{
    public class LinkFolderAttribute : Attribute
    {
        public readonly string folder;

        public LinkFolderAttribute(string folder)
        {
            this.folder = folder;
        }
    }

    public class FlatHierarchy : Attribute
    {
        public FlatHierarchy()
        {
        }
    }
}