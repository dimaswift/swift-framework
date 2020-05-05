
using System;

namespace SwiftFramework.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ModuleGroupAttribute : Attribute
    {
        public readonly string GroupId;

        public ModuleGroupAttribute(string groupId)
        {
            GroupId = groupId;
        }
    }
}
