using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class RuntimeModuleFactory : IModuleFactory
    {
        private static readonly List<Type> definedTypes = new List<Type>();

        public RuntimeModuleFactory()
        {
            CacheTypes();
        }

        private static void CacheTypes()
        {
            if (definedTypes.Count == 0)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(a == null)
                    {
                        continue;
                    }
                    foreach (Type type in a.DefinedTypes)
                    {
                        definedTypes.Add(type);
                    }
                }
            }
        }

        private IModule CreateAnyImplementation(Type interfaceType, ModuleConfigLink configLink)
        {
            IModule behaviour = FindBehaviourImplementation(interfaceType);
            if(behaviour != null)
            {
                if(behaviour.GetType().GetCustomAttribute<ConfigurableAttribute>() != null)
                {
                    behaviour.GetType().GetMethod("SetConfig").Invoke(behaviour, new object[] { configLink });
                }
                return behaviour;
            }
            else
            {
                return FindPureImplementation(interfaceType, configLink);
            }
        }

        private IModule FindBehaviourImplementation(Type interfaceType)
        {
            foreach (var type in definedTypes)
            {
                if (type.IsClass && interfaceType.IsAssignableFrom(type) && typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    return CreateModule(ModuleLink.Create(interfaceType, type));
                }
            }
            return default;
        }

        private IModule FindPureImplementation(Type interfaceType, ModuleConfigLink configLink)
        {
            foreach (var type in definedTypes)
            {
                if (type.IsClass && interfaceType.IsAssignableFrom(type) && typeof(MonoBehaviour).IsAssignableFrom(type) == false)
                {
                    try
                    {
                        if (type.GetCustomAttribute<ConfigurableAttribute>() != null)
                        {
                            return (IModule)Activator.CreateInstance(type, configLink);
                        }
                        else
                        {
                            return (IModule)Activator.CreateInstance(type);
                        }

                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            return default;
        }

        private bool IsConfigurable(Type moduleType)
        {
            return moduleType != null && moduleType.GetCustomAttribute<ConfigurableAttribute>() != null;
        }

        public static Type FindDefaultModuleImplementation(Type moduleInterface)
        {
            CacheTypes();
            foreach (Type type in definedTypes)
            {
                if (type.IsClass && moduleInterface.IsAssignableFrom(type) && type.GetCustomAttribute<DefaultModuleAttribute>() != null)
                {
                    return type;
                }
            }
            return null;
        }

        public static Type FindFirstModuleImplementation(Type moduleInterface)
        {
            CacheTypes();
            foreach (Type type in definedTypes)
            {
                if (type.IsClass && moduleInterface.IsAssignableFrom(type))
                {
                    return type;
                }
            }
            return null;
        }

        public IModule CreateModule(ModuleLink moduleLink)
        {
            Type implementationType = moduleLink.ImplementationType;
            if (implementationType == null)
            {
                if (moduleLink.InterfaceType == null)
                {
                    return null;
                }
                return CreateAnyImplementation(moduleLink.InterfaceType, moduleLink.ConfigLink);
            }
            ModuleConfigLink configLink = moduleLink.ConfigLink;
            bool isConfigurable = IsConfigurable(moduleLink.ImplementationType) && configLink != null && configLink.HasValue;

            if (typeof(BehaviourModule).IsAssignableFrom(implementationType))
            {
                BehaviourModule module = new GameObject(implementationType.Name).AddComponent(implementationType).GetComponent<BehaviourModule>();
                if (Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(module.gameObject);
                }
                else
                {
                    module.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                if (isConfigurable)
                {
                    Debug.Log($"{module}"); 
                    module.SetConfig(configLink);
                }
                return module.GetComponent<IModule>();
            }

            try
            {
                if (isConfigurable)
                {
                    return (IModule)Activator.CreateInstance(implementationType, new object[] { configLink });
                }
                else
                {
                    return (IModule)Activator.CreateInstance(implementationType);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public T CreateModule<T>() where T : IModule
        {
            return (T) CreateAnyImplementation(typeof(T), null);
        }

        public virtual IEnumerable<ModuleLink> GetModuleLinks()
        {
            yield break;
        }

        IPromise<T> IModuleFactory.CreateModule<T>()
        {
            return Promise<T>.Resolved(CreateModule<T>());
        }

        IPromise<IModule> IModuleFactory.CreateModule(ModuleLink moduleLink)
        {
            return Promise<IModule>.Resolved(CreateModule(moduleLink));
        }
    }
}
