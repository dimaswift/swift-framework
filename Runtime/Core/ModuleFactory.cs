using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core
{
    public class ModuleFactory : IModuleFactory
    {
        private readonly List<ModuleManifest> modules = new List<ModuleManifest>();

        public IPromise Init()
        {
            Promise promise = Promise.Create();
            AssetCache.PreloadAll(AddrLabels.Module).Always(manifests =>
            {
                modules.Clear();
                foreach (Object manifest in manifests)
                {
                    if (manifest is ModuleManifest moduleManifest)
                    {
                        if (moduleManifest.State == ModuleState.Enabled)
                        {
                            modules.Add(moduleManifest);
                        }
                    }
                }
                promise.Resolve();
            });
            return promise;
            
        }

        public IPromise<IModule> CreateModule(ModuleLink moduleLink)
        {
            return moduleLink.CreateModule();
        }

        public IEnumerable<ModuleLink> GetDefinedModules(ModuleLoadType loadType)
        {
            foreach (ModuleManifest moduleManifest in modules)
            {
                if (loadType.HasFlag(moduleManifest.LoadType))
                {
                    yield return moduleManifest.Link;
                }
            }
        }
    }
}