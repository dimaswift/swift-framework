using System.Collections.Generic;

namespace Swift.Core
{
    public interface IModuleFactory
    {
        IPromise Init();
        IPromise<IModule> CreateModule(ModuleLink moduleLink);
        IEnumerable<ModuleLink> GetDefinedModules(ModuleLoadType loadType);
    }
}