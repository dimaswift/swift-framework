using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface IModuleFactory
    {
        IPromise Init();
        IPromise<IModule> CreateModule(ModuleLink moduleLink);
        IEnumerable<ModuleLink> GetDefinedModules(ModuleLoadType loadType);
    }
}