
using System;

namespace Swift.Core
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

    [AttributeUsage(AttributeTargets.Interface)]
    public class BuiltInModuleAttribute : Attribute
    {
        public BuiltInModuleAttribute()
        {

        }
    }
}
