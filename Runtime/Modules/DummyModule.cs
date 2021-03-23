using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class DummyModule : IModule
    {
        public IEnumerable<ModuleLink> GetDependencies()
        {
            yield break;
        }

        public void SetUp(IApp app)
        {

        }

        public virtual Signal<ReInitializationResult> HandleReInitialization() => Signal<ReInitializationResult>.PreFired(ReInitializationResult.None);

        public IPromise InitModule()
        {
            return Promise.Resolved();
        }

        public IPromise Init()
        {
            Debug.LogWarning($"Module {GetType().Name} not implemented!");
            return Promise.Resolved();
        }

        public void Unload()
        {
            
        }
    }
}
