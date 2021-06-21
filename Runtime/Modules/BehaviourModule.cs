using Swift.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Swift.Core
{
    public abstract class BehaviourModule : MonoBehaviour
    {
        private Promise initPromise = null;
        
        public virtual Signal<ReInitializationResult> HandleReInitialization() => Signal<ReInitializationResult>.PreFired(ReInitializationResult.None);
        
        protected IApp App { get; private set; }

        protected bool Initialized => initPromise != null && initPromise.CurrentState == PromiseState.Resolved;

        public IPromise InitPromise => initPromise;

        private ModuleConfigLink configLink;

        public T GetModuleConfig<T>() where T : ModuleConfig
        {
            return configLink != null && configLink.HasValue ? configLink.Value as T : null;
        }

        public IPromise<T> GetModuleConfigAsync<T>() where T : ModuleConfig
        {
            Promise<T> promise = Promise<T>.Create();
         
            if (configLink == null)
            {
                promise.Reject(new KeyNotFoundException("Module config not found"));
                return promise;
            }

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
            foreach (Type depType in Module.GetDependenciesForType(GetType()))
            {
                yield return App.GetModuleLink(depType);
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

            initPromise = GetInitPromise() as Promise;
            initPromise.Done(() =>
            {
                OnInit();
            });
           
            return initPromise;
        }

        protected virtual void OnInit() { }

        protected virtual IPromise GetInitPromise()
        {
            return Promise.Resolved();
        }

        public virtual void Unload()
        {
            Destroy(gameObject);
        }
    }
}
