using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SwiftFramework.Core
{
    [Serializable]
    public class ModuleLink
    {
        public bool HasImplementation => ImplementationType != null;
        public BehaviourModuleLink BehaviourLink => behaviourLink;
        public ModuleConfigLink ConfigLink => configLink;

        private List<ModuleConfig> list;

        public Type InterfaceType
        {
            get
            {
                try
                {
                    return Type.GetType(interfaceType);
                }
                catch
                {
                    return null;
                }
            }

        }

        public Type ImplementationType
        {
            get
            {
                try
                {
                    return Type.GetType(implementationType);
                }
                catch
                {
                    return null;
                }
            }

        }

        public ModuleLink()
        {

        }

        private ModuleLink(Type implementationType, Type interfaceType, BehaviourModuleLink behaviourLink = null, ModuleConfigLink configLink = null)
        {
            this.implementationType = implementationType.AssemblyQualifiedName;
            this.interfaceType = interfaceType.AssemblyQualifiedName;
            this.behaviourLink = behaviourLink;
            this.configLink = configLink;
        }

   
        [SerializeField] private string implementationType;
        [SerializeField] private string interfaceType;
        [SerializeField] private BehaviourModuleLink behaviourLink;
        [SerializeField] private ModuleConfigLink configLink;

        private IModule module;
        private static readonly RuntimeModuleFactory runtimeModuleFactory = new RuntimeModuleFactory();

        public static ModuleLink Create<T>()
        {
            return Create(typeof(T));
        }

        public void SetConfigPath(string path)
        {
            configLink = Link.Create<ModuleConfigLink>(path);
        }

        public static ModuleLink Create(Type type)
        {
            return new ModuleLink(type);
        }

        public static ModuleLink Create<T>(Type implementationType, BehaviourModuleLink behaviourLink = null, ModuleConfigLink configLink = null)
        {
            return new ModuleLink(implementationType, typeof(T), behaviourLink, configLink);
        }

        public static ModuleLink Create(Type implementationType, Type interfaceType, BehaviourModuleLink behaviourLink = null, ModuleConfigLink configLink = null)
        {
            return new ModuleLink(implementationType, interfaceType, behaviourLink, configLink);
        }

        private ModuleLink(Type interfaceType)
        {
            this.interfaceType = interfaceType.AssemblyQualifiedName;
        }

        public T GetModule<T>() where T : IModule
        {
            if (module != null)
            {
                return (T)module;
            }
            module = App.Core.GetModule<T>(this);
            return (T)module;
        }

        public IPromise<IModule> CreateModule(Type interfaceType)
        {
            bool isConfigurable = HasImplementation
                && configLink != null
                && configLink.HasValue
                && ImplementationType.GetCustomAttribute<ConfigurableAttribute>() != null;

            Promise<IModule> promise = Promise<IModule>.Create();

            if (behaviourLink != null && behaviourLink.HasValue)
            {
                behaviourLink.Load(behaviour => 
                {
                    BehaviourModule moduleInstanceObject = UnityEngine.Object.Instantiate(behaviour.gameObject).GetComponent<BehaviourModule>();
                    UnityEngine.Object.DontDestroyOnLoad(moduleInstanceObject.gameObject);
                    moduleInstanceObject.name = behaviour.name;
                    if (isConfigurable)
                    {
                        moduleInstanceObject.SetConfig(configLink);
                    }
                    promise.Resolve(moduleInstanceObject.GetComponent(interfaceType) as IModule);
                },
                e => promise.Reject(e));
            }
            else
            {
                promise.Resolve(runtimeModuleFactory.CreateModule(this));
            }
            return promise;
        }

        public IPromise<T> CreateModule<T>() where T : IModule
        {
            Promise<T> promise = Promise<T>.Create();
            
            CreateModule(typeof(T)).Then(m => { promise.Resolve((T)m); }).Catch(e => promise.Reject(e));

            return promise;
        }

        public static bool operator == (ModuleLink a, ModuleLink b)
        {
            return a?.behaviourLink == b?.behaviourLink
                && a?.interfaceType == b?.interfaceType
                && a?.implementationType == b?.implementationType;
        }



        public static bool operator !=(ModuleLink a, ModuleLink b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            ModuleLink other = obj as ModuleLink;
            return other == this;
        }

        public IPromise<IModule> CreateModule()
        {
            return CreateModule(InterfaceType);
        }

        public override string ToString()
        {
            string result = "";
            if(InterfaceType != null)
            {
                result += $"Interface: {InterfaceType.Name}";
            }
            if (ImplementationType != null)
            {
                result += $", Implementation: {ImplementationType.Name}";
            }
            if (configLink != null)
            {
                result += $", Config: {configLink.GetPath()}";
            }
            if (behaviourLink != null)
            {
                result += $", Behaviour: {behaviourLink.GetPath()}";
            }
            return string.IsNullOrEmpty(result) ? "empty" : result;
        }

        public override int GetHashCode()
        {
            var hashCode = -54518709;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(implementationType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(interfaceType);
            hashCode = hashCode * -1521134295 + EqualityComparer<BehaviourModuleLink>.Default.GetHashCode(behaviourLink);
            return hashCode;
        }
    }
}
