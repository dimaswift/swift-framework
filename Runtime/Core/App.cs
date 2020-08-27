using System;
using System.Collections.Generic;
using SwiftFramework.Core.Pooling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core
{
    public enum AppState
    {
        Asleep = 0,
        Loading = 1,
        CoreModulesInitialized = 2,
        AssetsPreloaded = 3,
        ModulesInitialized = 4,
        Disposed = 5
    }

    public sealed class App : IApp
    {
        internal static event Action OnDomainReloaded = () => { };
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDomain()
        {
            unloadPending = Core != null;
            OnDomainReloaded();
        }
        
        public static bool Initialized => initPromise.CurrentState == PromiseState.Resolved;

        public static IPromise InitPromise => initPromise;

        public static long Now => Core.Clock.Now.Value;

        public static event ValueHandler<long> OnClockTick 
        {
            add => Core.Clock.Now.OnValueChanged += value;
            remove => Core.Clock.Now.OnValueChanged -= value;
        }

        public static void WaitForState(AppState state, Action action)
        {
            if (unloadPending)
            {
                if (awaitingActionsAfterUnload.ContainsKey(state) == false)
                {
                    awaitingActionsAfterUnload.Add(state, new List<Action>());
                }
                awaitingActionsAfterUnload[state].Add(action);
                return;
            }
            
            if ((int) State >= (int) state)
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
        
        public static void ExecuteOnLoad(Action action)
        {
            WaitForState(AppState.ModulesInitialized, action);
        }

        private static readonly Dictionary<AppState, List<Action>> awaitingActions =
            new Dictionary<AppState, List<Action>>();
        
        private static readonly Dictionary<AppState, List<Action>> awaitingActionsAfterUnload =
            new Dictionary<AppState, List<Action>>();

        private static AppState State { get; set; }

        private static Promise initPromise = Promise.Create();

        private static bool unloadPending;

        public static App Core { get; private set; }

        private App()
        {
            Core = this;
        }

        public void Unload()
        {
            foreach (KeyValuePair<ModuleLink, IModule> m in readyModules)
            {
                try
                {
                    m.Value.Unload();
                }
                catch
                {
                    // ignored
                }
            }

            SetState(AppState.Disposed);
            awaitingActions.Clear();
            Destroy();
        }

        public static IPromise Create(IBoot boot, ILogger logger, IModuleFactory moduleFactory, bool debugMode)
        {
            if (unloadPending && Core != null)
            {
                Core.Unload();
                unloadPending = false;
                OnDomainReloaded = () => { };
                
                foreach (KeyValuePair<AppState,List<Action>> pair in awaitingActionsAfterUnload)
                {
                    awaitingActions.Add(pair.Key, pair.Value);
                }
                awaitingActionsAfterUnload.Clear();
                Core = null;
            }

            if (Core == null)
            {
                Core = new App();
                return Core.Init(boot, logger, moduleFactory, debugMode);
            }

            return initPromise;
        }

        public ILocalizationManager Local => GetCachedModule(ref localizationManager);
        public IClock Clock => GetCachedModule(ref clock);
        public INetworkManager Net => GetCachedModule(ref networkManager);
        public ICoroutineManager Coroutine => GetCachedModule(ref coroutine);
        public ISaveStorage Storage => GetCachedModule(ref storage);
        public IViewFactory Views => GetCachedModule(ref views);
        public IWindowsManager Windows => GetCachedModule(ref windowsManager);

        public IBoot Boot => boot;
        public ITimer Timer => GetCachedModule(ref timer);

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

        private readonly Dictionary<ModuleLink, IPromise<IModule>> createdModules =
            new Dictionary<ModuleLink, IPromise<IModule>>();

        private readonly Dictionary<ModuleLink, IModule> readyModules = new Dictionary<ModuleLink, IModule>();
        private readonly Dictionary<Type, IModule> readyModulesDict = new Dictionary<Type, IModule>();
        private bool initializingStarted = false;
        private bool debugMode;

        private IPromise Init(IBoot boot, ILogger logger, IModuleFactory moduleFactory, bool debugMode)
        {
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

            if (initializingStarted)
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

            initializingStarted = true;
            SetState(AppState.Loading);

            moduleFactory.Init().Then(() =>
                {
                    AssetCache.PreloadAll(AddrLabels.Prewarm).Done(assets =>
                    {
                        SetState(AppState.AssetsPreloaded);

                        List<IPromise<IModule>> coreModules =
                            new List<IPromise<IModule>>(GetBuiltInModulesInitPromises());

                        Promise.All(coreModules).Always(() =>
                        {
                            SetState(AppState.CoreModulesInitialized);
                            InitModules().Then(() =>
                                {
                                    SetState(AppState.ModulesInitialized);
                                })
                                .Catch(logger.LogException);
                        });
                    });
                })
                .Catch(logger.LogException);


            return initPromise;
        }

        private static void SetState(AppState state)
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

        private IEnumerable<IPromise<IModule>> GetBuiltInModulesInitPromises()
        {
            yield return CreateModule(GetModuleLink<ISaveStorage>());
            yield return CreateModule(GetModuleLink<ITimer>());
            yield return CreateModule(GetModuleLink<ICoroutineManager>());
            yield return CreateModule(GetModuleLink<IClock>());
            yield return CreateModule(GetModuleLink<IViewFactory>());
            yield return CreateModule(GetModuleLink<INetworkManager>());
            yield return CreateModule(GetModuleLink<ILocalizationManager>());
        }

        private IPromise<IModule> CreateCoreModule<TInt, TImp>() where TImp : IModule, new() where TInt : IModule
        {
            Promise<IModule> promise = Promise<IModule>.Create();

            IModule module = new TImp();
            module.SetUp(this);

            ModuleLink link = ModuleLink.Create(typeof(TImp), typeof(TInt));

            createdModules.Add(link, promise);

            module.Init().Done(() =>
            {
                readyModules.Add(link, module);
                promise.Resolve(module);
            });

            return promise;
        }

        private IPromise<IModule> CreateBehaviourCoreModule<TInt, TImp>()
            where TImp : BehaviourModule where TInt : IModule
        {
            Promise<IModule> promise = Promise<IModule>.Create();
            GameObject go = new GameObject(typeof(TImp).Name);
            Object.DontDestroyOnLoad(go);
            TInt module = go.AddComponent<TImp>().GetComponent<TInt>();
            module.SetUp(this);
            module.Init().Done(() =>
            {
                readyModules.Add(ModuleLink.Create(typeof(TImp), typeof(TInt)), module);
                promise.Resolve(module);
            });

            return promise;
        }

        private IPromise InitModules()
        {
            List<IPromise<IModule>> promises = new List<IPromise<IModule>>();

            foreach (ModuleLink coreModuleLink in moduleFactory.GetDefinedModules(ModuleLoadType.OnInitialize))
            {
                promises.Add(CreateModule(coreModuleLink));
            }

            Promise.All(promises).Always(() => { initPromise.Resolve(); });

            return initPromise;
        }

        public T GetModule<T>(ModuleLink moduleLink) where T : class, IModule
        {
            if (readyModules.TryGetValue(moduleLink, out IModule module))
            {
                return (T) module;
            }

            return default;
        }

        public T GetModule<T>() where T : class, IModule
        {
            Type type = typeof(T);

            if (readyModulesDict.TryGetValue(type, out IModule module))
            {
                return (T) module;
            }

            foreach (KeyValuePair<ModuleLink, IModule> m in readyModules)
            {
                if (m.Value is T value)
                {
                    readyModulesDict.Add(type, m.Value);
                    return value;
                }
            }

            return default;
        }
        
        public bool UnloadModule<T>() where T : class, IModule
        {
            Type type = typeof(T);

            ModuleLink link = GetModuleLink(type);

            if (link == null)
            {
                return false;
            }

            if (readyModulesDict.TryGetValue(type, out IModule module))
            {
                module.Unload();
                readyModulesDict.Remove(type);

                if (createdModules.ContainsKey(link))
                {
                    createdModules.Remove(link);
                }

                if (readyModules.ContainsKey(link))
                {
                    readyModules.Remove(link);
                }
                
                return true;
            }

            return false;
        }


        public IPromise<T> LoadModule<T>() where T : class, IModule
        {
            Promise<T> promise = Promise<T>.Create();
            Type type = typeof(T);
            if (IsCreated(GetModuleLink(type), out IPromise<IModule> modulePromise))
            {
                modulePromise.Then(m => promise.Resolve(m as T), e => promise.Reject(e));
                return promise;
            }
            CreateModule(GetModuleLink(type)).Then(m => promise.Resolve(m as T), e => promise.Reject(e));
            return promise;
        }


        public IPromise<IModule> CreateModule(ModuleLink moduleLink)
        {
            if (moduleLink == null)
            {
                return Promise<IModule>.Rejected(new Exception($"Module not found!"));
            }

            if (moduleLink.InterfaceType == null)
            {
                return Promise<IModule>.Rejected(new Exception($"Module interface type not found!"));
            }
            
            Promise<IModule> result = Promise<IModule>.Create();

            if (IsCreated(moduleLink, out IPromise<IModule> i))
            {
                i.Then(m => { m.Init().Done(() => result.Resolve(m)); })
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
                    if (depLink == null)
                    {
                        continue;
                    }
                    if (debugMode)
                    {
                        logger.Log(
                            $"Trying to resolve dependency: <b>{depLink.InterfaceType?.Name}</b> for <b>{moduleLink.InterfaceType?.Name}</b>");
                    }

                    if (IsCreated(depLink, out IPromise<IModule> depModCreatePromise))
                    {
                        Promise depInitPromise = Promise.Create();

                        depModCreatePromise.Done(depModule =>
                        {
                            bool circularDependency = false;
                            foreach (ModuleLink subDep in depModule.GetDependencies())
                            {
                                if (subDep == moduleLink)
                                {
                                    logger.LogError(
                                        $"Circular dependency detected: {depLink.ImplementationType.Name} and {moduleLink.ImplementationType.Name} depend on each other");
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

                            CreateModule(depLink).Then(m => { newDepModuleInitPromise.Resolve(); })
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
                            logger.Log(
                                $"Cannot resolve dependency for module: {moduleLink}. Cannot find dependency!");
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
                string error =
                    $"Cannot create module of type <b>{moduleLink.ImplementationType?.Name}</b> " +
                    $"that implements <b>{moduleLink.InterfaceType?.Name}</ b >.";
                logger.Log(error);
                result.Reject(new EntryPointNotFoundException(error));
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

        public T GetCachedModule<T>(ref T cachedModule) where T : class, IModule
        {
            if (cachedModule == null)
            {
                cachedModule = GetModule<T>();
            }

            return cachedModule;
        }

        public ModuleLink GetModuleLink<T>() where T : class, IModule
        {
            return GetModuleLink(typeof(T));
        }

        public ModuleLink GetModuleLink(Type type)
        {
            foreach (ModuleLink link in moduleFactory.GetDefinedModules(
                ModuleLoadType.OnDemand | ModuleLoadType.OnInitialize))
            {
                if (link.InterfaceType == type)
                {
                    return link;
                }
            }

            logger.LogWarning($"Module of type {type.Name} not defined!");
            return null;
        }

        public IPromise MakeTransition(IPromise promiseToWait, Action action)
        {
            Promise result = Promise.Create();

            Core.GetCachedModule(ref windowsManager);

            if (windowsManager == null)
            {
                action();
                promiseToWait.Always(() => result.Resolve());
                return result;
            }

            windowsManager.MakeTransition(promiseToWait, action).Always(() => { result.Resolve(); });

            return result;
        }

        private void Destroy()
        {
            awaitingActions.Clear();
            AssetCache.Dispose();
            Pool.DisposeAllPools();
            Core = null;
            initPromise = Promise.Create();
            readyModules.Clear();
            createdModules.Clear();
            readyModulesDict.Clear();
        }
    }
}