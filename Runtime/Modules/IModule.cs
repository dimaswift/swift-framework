using System.Collections.Generic;

namespace Swift.Core
{
    public interface IModule
    {
        IPromise Init();
        void Unload();
        IEnumerable<ModuleLink> GetDependencies();
        void SetUp(IApp app);
        Signal<ReInitializationResult> HandleReInitialization();
        
    }
}
