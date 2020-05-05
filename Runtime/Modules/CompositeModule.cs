using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public abstract class CompositeModule : BehaviourModule
    {
        private readonly List<IModule> subModules = new List<IModule>();

        protected override void OnSetUp(IApp app)
        {
            base.OnSetUp(app);
            GetComponentsInChildren(subModules);
            subModules.RemoveAll(m => m == GetComponent<IModule>());
            foreach (var subModule in subModules)
            {
                subModule.SetUp(app);
            }
        }

        protected IEnumerable<T> GetSubmodules<T>() where T : class, IModule
        {
            foreach (var subModule in subModules)
            {
                if (subModule is T)
                {
                    yield return subModule as T;
                }
            }
        }

        protected T GetSubmodule<T>() where T : class, IModule
        {
            foreach (var subModule in subModules)
            {
                if (subModule is T)
                {
                    return subModule as T;
                }
            }
            return default;
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
