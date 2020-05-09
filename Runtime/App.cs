using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    public enum AppState
    {
        Asleep = 0,
        Loading = 1,
        CoreModulesInitialized = 2,
        AssetsPreloaded = 3,
        ModulesInitialized = 4
    }

    public abstract class App : IApp
    {
        public static long Now => Core.Clock.Now.Value;

        public static event ValueHanlder<long> OnClockTick
        {
            add
            {
                Core.Clock.Now.OnValueChanged += value;
            }
            remove
            {
                Core.Clock.Now.OnValueChanged -= value;
            }
        }

        protected static bool UnloadPending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDomain()
        {
            App app = Core as App;
            if (app != null)
            {
                UnloadPending = true;
            }
        }

        public static AppState State { get; protected set; }

        public static IPromise InitPromise
        {
            get
            {
                return initPromise;
            }
        }

        public static bool Initialized => initPromise.CurrentState == PromiseState.Resolved;

        protected static Promise initPromise = Promise.Create();

        public virtual IPromise Ready => initPromise;

        public static IApp Core { get; protected set; }

        public abstract IBoot Boot { get; }

        public abstract ILocalizationManager Local { get; }

        public abstract IClock Clock { get; }

        public abstract INetworkManager Net { get; }

        public abstract ICoroutineManager Coroutine { get; }

        public abstract IViewFactory Views { get; }

        public abstract ISaveStorage Storage { get; }

        public abstract ITimer Timer { get; }

        public abstract IWindowsManager Windows { get; }

        public abstract IPromise<IModule> CreateModule(ModuleLink moduleLink);

        public abstract T GetModule<T>(ModuleLink moduleLink) where T : IModule;

        public abstract T GetModule<T>() where T : IModule;

        public abstract ModuleLink GetModuleLink<T>() where T : IModule;

        public abstract T GetCachedModule<T>(ref T cachedModule) where T : IModule;

        public abstract void Unload();

        protected abstract void Destroy();

        public static void WaitForState(AppState state, Action action)
        {
            if ((int)State >= (int)state)
            {
                action();
                return;
            }

            if (awaitingActions.ContainsKey(state) == false)
            {
                awaitingActions.Add(state, new List<Action>());
            }

            awaitingActions[state].Add(action);
        }

        protected static Dictionary<AppState, List<Action>> awaitingActions = new Dictionary<AppState, List<Action>>();

        protected static void SetState(AppState state)
        {
            State = state;
            if (awaitingActions.TryGetValue(state, out List<Action> list))
            {
                awaitingActions.Remove(state);
                foreach (Action action in list)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            State = state;
        }

        public abstract ModuleLink GetModuleLink(Type type);

        public abstract IPromise MakeTransition(IPromise waitForPromise, Action action);

        protected App()
        {
            Core = this;
            State = AppState.Loading;
        }
    }

    public class App<M> : App where M : App<M>, new()
    {
        private static M main = null;

        public static M Main
        {
            get
            {
                if (main == null)
                {
                    throw new InvalidOperationException($"{typeof(M).Name} is not created. Call {typeof(M).Name}.Create() first!");
                }
                return main;
            }
        }


        public override void Unload()
        {
            if (main == null)
            {
                return;
            }
            foreach (var m in main.readyModules)
            {
                m.Value.Unload();
            }
            Destroy();
        }

        public static IPromise Create(IBoot boot, ILogger logger, IModuleFactory moduleFactory, bool debugMode)
        {
            if (UnloadPending)
            {
                main.Destroy();
                UnloadPending = false;
            }

            if (main == null)
            {
                main = new M();
                Core = main;
                return main.Init(boot, logger, moduleFactory, debugMode);
            }
            return InitPromise;
        }

        public override ILocalizationManager Local => GetCachedModule(ref localizationManager);
        public override IClock Clock => GetCachedModule(ref clock);
        public override INetworkManager Net => GetCachedModule(ref networkManager);
        public override ICoroutineManager Coroutine => GetCachedModule(ref coroutine);
        public override ISaveStorage Storage => GetCachedModule(ref storage);
        public override IViewFactory Views => GetCachedModule(ref views);
        public override IWindowsManager Windows => GetCachedModule(ref windowsManager);

        public override IBoot Boot => boot;
        public override ITimer Timer => GetCachedModule(ref timer);

        private ILocalizationManager localizationManager;
        private IClock clock;
        private INetworkManager networkManager;
        private ICoroutineManager coroutine;
        private ITimer timer;
        private ISaveStorage storage;
        private IViewFactory views;

        private IModuleFactory moduleFactory;
        private IBoot boot;
        private ILogger logger;
        private IWindowsManager windowsManager;


        private readonly Dictionary<ModuleLink, IPromise<IModule>> createdModules = new Dictionary<ModuleLink, IPromise<IModule>>();
        private readonly Dictionary<ModuleLink, IModule> readyModules = new Dictionary<ModuleLink, IModule>();

        private bool initialializingStarted = false;
        private bool debugMode;

        public IPromise Init(IBoot boot, ILogger logger, IModuleFactory moduleFactory, bool debugMode)
        {
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

            if (initialializingStarted)
            {
                return initPromise;
            }

            this.logger = logger;
            this.debugMode = debugMode;
            this.boot = boot;
            this.moduleFactory = moduleFactory;

            if (moduleFactory == null)
            {
                return Promise.Rejected(new NullReferenceException("IModuleFactory is null. Cannot create App!"));
            }

            initialializingStarted = true;
            SetState(AppState.Loading);

            AddrCache.PreloadAll(AddrLabels.Prewarm).Done(assets =>
            {
                SetState(AppState.AssetsPreloaded);

                List<IPromise<IModule>> coreModules = new List<IPromise<IModule>>(GetCoreModulesInitPromises());

                Promise.All(coreModules).Always(() =>
                {
                    SetState(AppState.CoreModulesInitialized);
                    InitModules().Then(() =>
                    {
                        SetState(AppState.ModulesInitialized);
                        OnInit();
                    })
                    .Catch(e => logger.LogException(e));
                });
            });

            return initPromise;
        }

        private IEnumerable<IPromise<IModule>> GetCoreModulesInitPromises()
        {
            yield return CreateCoreModule<ISaveStorage, SaveStorageManager>();
            yield return CreateBehaviourCoreModule<ITimer, CoroutineTimer>();
            yield return CreateBehaviourCoreModule<ICoroutineManager, CoroutineManager>();
            yield return CreateBehaviourCoreModule<IClock, Clock>();
            yield return CreateBehaviourCoreModule<IViewFactory, ViewFactory>();
            yield return CreateBehaviourCoreModule<INetworkManager, NetworkManager>();
            yield return CreateModule(GetModuleLink<ILocalizationManager>());
        }

        private IPromise<IModule> CreateCoreModule<TInt, TImp>() where TImp : IModule, new() where TInt : IModule
        {
            Promise <IModule> promise = Promise<IModule>.Create();

            IModule module = new TImp();
            module.SetUp(this);

            var link = ModuleLink.Create(typeof(TImp), typeof(TInt));

            createdModules.Add(link, promise);

            module.Init().Done(() =>
            {
                readyModules.Add(link, module);
                promise.Resolve(module);
            });

            return promise;
        }

        private IPromise<IModule> CreateBehaviourCoreModule<TInt, TImp>() where TImp : BehaviourModule where TInt : IModule
        {
            Promise<IModule> promise = Promise<IModule>.Create();
            var go = new GameObject(typeof(TImp).Name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            TInt module = go.AddComponent<TImp>().GetComponent<TInt>();
            module.SetUp(this);
            module.Init().Done(() => 
            {
                readyModules.Add(ModuleLink.Create(typeof(TImp), typeof(TInt)), module);
                promise.Resolve(module);
            });

            return promise;
        }

        protected virtual void OnInit()
        {

        }

        private IPromise InitModules()
        {
            List<IPromise<IModule>> promises = new List<IPromise<IModule>>();

            foreach (ModuleLink coreModuleLink in moduleFactory.GetModuleLinks())
            {
                promises.Add(CreateModule(coreModuleLink)); 
            }

            Promise.All(promises).Always(() => 
            {
                initPromise.Resolve();
            });

            return initPromise;
        }

        public override T GetModule<T>(ModuleLink moduleLink)
        {
            if (readyModules.TryGetValue(moduleLink, out IModule module))
            {
                return (T)module;
            }
            return default;
        }

        public override T GetModule<T>()
        {
            foreach (var m in readyModules)
            {
                if (typeof(T).IsAssignableFrom(m.Value.GetType()))
                {
                    return (T)m.Value;
                }
            }
            return default;
        }


        public override IPromise<IModule> CreateModule(ModuleLink moduleLink)
        {
            if (moduleLink == null)
            {
                return Promise<IModule>.Rejected(new Exception("Module not found!"));
            }

            Promise<IModule> result = Promise<IModule>.Create();

            if (IsCreated(moduleLink, out IPromise<IModule> i))
            {
                i.Then(m =>
                {
                    m.Init().Done(() => result.Resolve(m));
                })
                .Catch(e => result.Reject(e));
                return result;
            }

            IPromise<IModule> createModulePromise = moduleFactory.CreateModule(moduleLink);

            createdModules.Add(moduleLink, result);

            if (debugMode)
            {
                logger.Log($"Trying to create {moduleLink.InterfaceType.Name}");
            }

            createModulePromise.Then(newModule =>
            {
                newModule.SetUp(this);

                List<IPromise> dependenciesInitPromiseList = new List<IPromise>();

                foreach (ModuleLink depLink in newModule.GetDependencies())
                {
                    if (debugMode)
                    {
                        logger.Log($"Trying to resolve dependency: <b>{depLink.InterfaceType.Name}</b> for <b>{moduleLink.InterfaceType.Name}</b>");
                    }
                    if (IsCreated(depLink, out IPromise<IModule> depModCreatePromise))
                    {
                        Promise depInitPromise = Promise.Create();

                        depModCreatePromise.Done(depModule =>
                        {
                            bool circularDependency = false;
                            foreach (var subDep in depModule.GetDependencies())
                            {
                                if (subDep == moduleLink)
                                {
                                    logger.LogError($"Circular dependency detected: {depLink.ImplementationType.Name} and {moduleLink.ImplementationType.Name} depend on each other");
                                    circularDependency = true;
                                    break;
                                }
                            }
                            if (circularDependency == false)
                            {
                                depModule.Init().Done(() => depInitPromise.Resolve());
                            }
                            else
                            {
                                depInitPromise.Resolve();
                            }
                        });

                        dependenciesInitPromiseList.Add(depInitPromise);
                    }
                    else
                    {
                        if (depLink != null)
                        {
                            Promise newDepModuleInitPromise = Promise.Create();

                            CreateModule(depLink).Then(_m =>
                            {
                                newDepModuleInitPromise.Resolve();
                            })
                            .Catch(e =>
                            {
                                logger.LogException(e);
                                logger.LogError($"Cannot resolve dependency {depLink} for module {moduleLink}");
                                newDepModuleInitPromise.Resolve();
                            });

                            dependenciesInitPromiseList.Add(newDepModuleInitPromise);
                        }
                        else
                        {
                            logger.Log($"Cannot resolve dependency for module: {moduleLink}. Cannot find dependency!");
                        }
                    }
                }

                Promise.All(dependenciesInitPromiseList.ToArray()).Then(() =>
                {
                    if (debugMode)
                    {
                        logger.Log($"Trying to initialize {newModule}");
                    }

                    IPromise init = newModule.Init();

                    init.Done(() =>
                    {
                        readyModules.Add(moduleLink, newModule);

                        result.ReportProgress(1);

                        result.Resolve(newModule);

                        if (debugMode)
                        {
                            logger.Log($"{newModule} initialized");
                        }
                    });

                    init.Catch(e =>
                    {
                        logger.LogException(e);
                        logger.LogError($"Cannot resolve dependencies for module {moduleLink}");
                        result.Reject(e);
                    });
                })
               .Catch(e =>
               {
                   logger.LogException(e);
                   logger.LogError($"Cannot resolve dependencies for module {moduleLink}");
                   result.Resolve(newModule);
               });
            })
            .Catch(e =>
            {
                logger.Log($"Cannot create module of type < b >{ moduleLink.ImplementationType?.Name}</ b > that implements < b >{ moduleLink.InterfaceType?.Name}</ b >.");
                result.Reject(new EntryPointNotFoundException($"Cannot create module of type <b>{moduleLink.ImplementationType?.Name}</b> that implements <b>{moduleLink.InterfaceType?.Name}</b>."));
            });

            return result;
        }

        private bool IsCreated(ModuleLink moduleLink, out IPromise<IModule> createPromise)
        {
            if (moduleLink == null)
            {
                createPromise = null;
                return false;
            }

            return createdModules.TryGetValue(moduleLink, out createPromise);
        }

        public override T GetCachedModule<T>(ref T cachedModule)
        {
            if (cachedModule == null)
            {
                foreach (var m in readyModules)
                {
                    if (m.Value is T)
                    {
                        cachedModule = (T)m.Value;
                        break;
                    }
                }
                if (cachedModule == null)
                {
                    logger.LogError($"Cannot get module <b>{typeof(T).Name}</b>! Not ready yet. Consider adding a dependency to calling module");
                }
            }
            return cachedModule;
        }

        public override ModuleLink GetModuleLink<T>()
        {
            return GetModuleLink(typeof(T));
        }

        public override ModuleLink GetModuleLink(Type type)
        {
            foreach (ModuleLink link in moduleFactory.GetModuleLinks())
            {
                if (link.InterfaceType == type)
                {
                    return link;
                }
            }
            logger.LogWarning($"Core Link of type {type.Name} not found");
            return null;
        }

        public override IPromise MakeTransition(IPromise promiseToWait, Action action)
        {
            Promise result = Promise.Create();

            Core.GetCachedModule(ref windowsManager);

            if (windowsManager == null)
            {
                action();
                promiseToWait.Always(() => result.Resolve());
                return result;
            }

            windowsManager.MakeTransition(promiseToWait, action).Always(() =>
            {
                result.Resolve();
            });

            return result;
        }

        protected override void Destroy()
        {
            awaitingActions.Clear();
            AddrCache.Dispose();
            Pooling.Pool.DisposeAllPools();
            main = null;
            Core = null;
            initPromise = Promise.Create();
        }
    }
}
