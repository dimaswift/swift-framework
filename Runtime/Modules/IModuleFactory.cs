using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface IModuleFactory
    {
        IPromise<T> CreateModule<T>() where T : IModule;
        IPromise<IModule> CreateModule(ModuleLink moduleLink);
        IEnumerable<ModuleLink> GetModuleLinks();
    }
}