using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SwiftFramework.Core
{
    public abstract class CompositeModule<T> : BehaviourModule where T :  IModule
    {
        [SerializeField] private Location location = Location.Children;
        
        private enum Location
        {
            Children = 0, 
            Resources = 1
        }
        
        private readonly List<T> subModules = new List<T>();

        protected override void OnSetUp(IApp app)
        {
            base.OnSetUp(app);

            switch (location)
            {
                case Location.Children:
                    GetComponentsInChildren(subModules);
                    break;
                case Location.Resources:
                {
                    foreach (T prefab in AssetCache.GetPrefabs<T>())
                    {
                        BehaviourModule module = prefab as BehaviourModule;
                        if (module == null)
                        {
                            Debug.LogError("Cannot use composite module: " + prefab + ". Has to be a BehaviourModule");
                            continue;
                        }
                        T moduleInstance = Instantiate(module.gameObject).GetComponent<T>();
                        subModules.Add(moduleInstance);
                    }

                    break;
                }
            }

            subModules.RemoveAll(m => m is CompositeModule<T>);
            foreach (T subModule in subModules)
            {
                subModule.SetUp(app);
            }
        }

        protected IEnumerable<T> GetSubmodules()
        {
            return subModules;
        }

        protected override IPromise GetInitPromise()
        {
            Promise initPromise = Promise.Create();

            InitNextModule(0, initPromise, App);

            return initPromise;
        }

        protected virtual void OnSubModuleInitialized(IModule subModule) { }

        private void InitNextModule(int current, Promise iniPromise, IApp app)
        {
            if (current >= subModules.Count)
            {
                iniPromise.Resolve();
                return;
            }
            subModules[current].Init().Then(() =>
            {
                OnSubModuleInitialized(subModules[current]);
                InitNextModule(++current, iniPromise, app);
            })
            .Catch(e =>
            {
                InitNextModule(++current, iniPromise, app);
            });
        }
    }
}
