using System;

namespace SwiftFramework.Core
{
    public interface IApp
    {
        IBoot Boot { get; }
        ISaveStorage Storage { get; }
        ILocalizationManager Local { get; }
        IClock Clock { get; }
        INetworkManager Net { get; }
        ICoroutineManager Coroutine { get; }
        IViewFactory Views { get; }
        ITimer Timer { get; }
        IWindowsManager Windows { get; }
        IPromise<IModule> CreateModule(ModuleLink moduleLink);
        T GetModule<T>(ModuleLink moduleLink) where T : class, IModule;
        T GetCachedModule<T>(ref T cachedModule) where T : class, IModule;
        T GetModule<T>() where T : class, IModule;
        IPromise<T> LoadModule<T>() where T : class, IModule;
        ModuleLink GetModuleLink<T>() where T : class, IModule;
        ModuleLink GetModuleLink(Type type);
        bool UnloadModule<T>() where T : class, IModule;
        void Unload();
        IPromise MakeTransition(IPromise waitForPromise, Action action);
    }
}

