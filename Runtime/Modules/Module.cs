using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Swift.Core
{
    public enum ReInitializationResult
    {
        None = 0,
        Failed = 1,
        Success = 2
    }
    
    public abstract class Module
    {
        protected Promise initPromise = null;
        
        public virtual Signal<ReInitializationResult> HandleReInitialization() => Signal<ReInitializationResult>.PreFired(ReInitializationResult.None);
        protected IApp App { get; private set; }

        protected bool Initialized => initPromise != null && initPromise.CurrentState == PromiseState.Resolved;

        public IPromise InitPromise => initPromise;

        private ModuleConfigLink configLink;

        protected virtual bool EnableDebugLog => false;

        public T GetModuleConfig<T>() where T : ModuleConfig
        {
            return configLink != null && configLink.HasValue ? configLink.Value as T : null;
        }

        public IPromise<T> GetModuleConfigAsync<T>() where T : ModuleConfig
        {
            Promise<T> promise = Promise<T>.Create();

            configLink.Load().Then(c =>
            {
                promise.Resolve(c as T);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public void SetConfig(ModuleConfigLink configLink)
        {
            this.configLink = configLink;
        }

        public virtual IEnumerable<ModuleLink> GetDependencies()
        {
            foreach (Type depType in GetDependenciesForType(GetType()))
            {
                yield return App.GetModuleLink(depType);
            }
        }

        public static IEnumerable<Type> GetDependenciesForType(Type moduleType)
        {
            if (moduleType == null)
            {
                yield break;
            }
            DependsOnModulesAttribute attr = moduleType.GetCustomAttribute<DependsOnModulesAttribute>();
            if (attr != null)
            {
                foreach (var type in attr.dependencies)
                {
                    yield return type;
                }
            }
        }

        public static IEnumerable<Type> GetOtherUsedModules(Type moduleType)
        {
            if (moduleType == null)
            {
                yield break;
            }
            UsesModuleAttribute attr = moduleType.GetCustomAttribute<UsesModuleAttribute>();
            if (attr != null)
            {
                foreach (var type in attr.dependencies)
                {
                    yield return type;
                }
            }
        }

        public void SetUp(IApp app)
        {
            App = app;
            OnSetUp(app);
        }

        protected virtual void OnSetUp(IApp app)
        {

        }

        public IPromise Init()
        {
            if (initPromise != null)
            {
                return initPromise;
            }

            try
            {
                initPromise = GetInitPromise() as Promise;
                initPromise.Done(() =>
                {
                    OnInit();
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return initPromise;
        }

        protected virtual void OnInit() { }

        protected virtual IPromise GetInitPromise()
        {
            return Promise.Resolved();
        }

        protected bool LogsEnabled()
        {
            if (configLink != null && configLink.HasValue)
            {
                return configLink.Value.enableDebugLog;
            }
            return EnableDebugLog;
        }

        protected void Log(string message)
        {
            if (LogsEnabled() == false)
            {
                return;
            }
            Debug.Log(message); 
        }

        protected void LogError(string message)
        {
            if (LogsEnabled() == false)
            {
                return;
            }
            Debug.LogError(message);
        }

        protected void LogWarning(string message)
        {
            if (LogsEnabled() == false)
            {
                return;
            }
            Debug.LogWarning(message);
        }

        public virtual void Unload()
        {

        }
    }
}
