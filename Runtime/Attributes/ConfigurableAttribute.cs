using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurableAttribute : PropertyAttribute
    {
        public readonly Type configType;

        public ConfigurableAttribute(Type configType)
        {
            this.configType = configType;
        }
    }
}
