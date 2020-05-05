using UnityEngine;

namespace SwiftFramework.Core
{
    public interface ICloudConfigManager : IModule
    {
        IPromise<T> LoadConfig<T>() where T : new();
        IPromise LoadConfig<T>(T configToOverride) where T : ScriptableObject;
    }
}
