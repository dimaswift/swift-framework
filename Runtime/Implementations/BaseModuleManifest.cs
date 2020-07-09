using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SwiftFramework.Core
{
  /*  [PrewarmAsset]
    public abstract class BaseModuleManifest : ScriptableObject, IModuleFactory
    {
        private readonly RuntimeModuleFactory runtimeModuleFactory = new RuntimeModuleFactory();

        public IEnumerable<(ModuleLink moduleLink, FieldInfo moduleFileds)> GetModuleFields()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (FieldInfo linkField in fields)
            {
                ModuleLink link = linkField.GetValue(this) as ModuleLink;
                if (link == null || link.ImplementationType == null)
                {
                    continue;
                }
                yield return (link, linkField);
            }
        }

        public IEnumerable<(FieldInfo field, ModuleLink link)> GetAllModuleLinks()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (FieldInfo linkField in fields)
            {
                ModuleLink link = linkField.GetValue(this) as ModuleLink;
                if (link == null)
                {
                    continue;
                }
                yield return (linkField, link);
            }
        }

        public IPromise<T> CreateModule<T>() where T : IModule
        {
            Promise<T> promise = Promise<T>.Create();
           
            ModuleLink moduleLink = FindModuleLink<T>();
            CreateModule(moduleLink).Then(m => promise.Resolve((T) m)).Catch(e => promise.Reject(e));

            return promise;
        }

        private ModuleLink FindModuleLink<T>()
        {
            foreach ((ModuleLink link, FieldInfo linkField) in GetModuleFields())
            {
                LinkFilterAttribute interfaceFilter = linkField.GetCustomAttribute<LinkFilterAttribute>();
                bool isConfigurable = link.ImplementationType.GetCustomAttribute<ConfigurableAttribute>() != null;
                if (link != null && interfaceFilter != null && interfaceFilter.interfaceType == typeof(T))
                {
                    return link;
                }
            }
            return null;
        }

        public IPromise<IModule> CreateModule(ModuleLink moduleLink)
        {
            Promise<IModule> promise = Promise<IModule>.Create();
        
            if (moduleLink != null)
            {
                moduleLink.CreateModule().Channel(promise);
                return promise;
            }

            IModule anyImplementation = runtimeModuleFactory.CreateModule(moduleLink);

            if (anyImplementation != null)
            {
                promise.Resolve(anyImplementation);
                return promise;
            }

            string err = $"Cannot find any implementation of type <b>{moduleLink}</b>!";

            Debug.LogError(err);

            promise.Reject(new NotImplementedException(err));

            return promise;
        }

        public IEnumerable<ModuleLink> GetModuleLinks()
        {
            foreach (var item in GetModuleFields())
            {
                yield return item.moduleLink;
            }
        }
    }*/
}
