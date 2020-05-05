using System;

namespace SwiftFramework.Core
{
    public interface IApp
    {
        IPromise Ready { get; }
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
        T GetModule<T>(ModuleLink moduleLink) where T : IModule;
        T GetCachedModule<T>(ref T cachedModule) where T : IModule;
        T GetModule<T>() where T : IModule;
        ModuleLink GetModuleLink<T>() where T : IModule;
        ModuleLink GetModuleLink(Type type);
        void Unload();
        IPromise MakeTransition(IPromise waitForPromise, Action action);
    }
}

