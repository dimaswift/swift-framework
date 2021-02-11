using System;

namespace SwiftFramework.Core
{
    [BuiltInModule]
    [ModuleGroup(ModuleGroups.Core)]
    public interface IViewFactory : IModule
    {
        bool IsLoaded(ViewLink link);
        T Create<T>(ViewLink link) where T : class, IView;

        IPromise WarmUp<T>(ViewLink link, int capacity) where T : class, IView;
        IPromise<T> CreateAsync<T>(ViewLink link) where T : class, IView;
        void CreateAsync<T>(ViewLink link, Action<T> onLoad, Action<Exception> onError = null) where T : class, IView;
        void ReturnEverythingToPool();
        T FindView<T>(ViewLink link) where T : class, IView;
    }
}
