using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IConfigManager : IModule
    {
        T GetPureConfig<T>() where T : new();
        T GetConfig<T>() where T : ScriptableObject;
    }
}
