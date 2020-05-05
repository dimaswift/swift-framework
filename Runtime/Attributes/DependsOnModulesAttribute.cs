using System;

namespace SwiftFramework.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DependsOnModulesAttribute : Attribute
    {
        public readonly Type[] dependencies;

        public DependsOnModulesAttribute(params Type[] dependencies)
        {
            this.dependencies = dependencies;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UsesModuleAttribute : Attribute
    {
        public readonly Type[] dependencies;

        public UsesModuleAttribute(params Type[] dependencies)
        {
            this.dependencies = dependencies;
        }
    }
}
