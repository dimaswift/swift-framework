using System;
using System.Collections;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class AppBoot : MonoBehaviour, IBoot
    {
        private Promise bootPromise = Promise.Create();
        
        public static IPromise BootUp()
        {
            AppBoot boot = new GameObject(nameof(AppBoot)).AddComponent<AppBoot>();

            boot.debugMode = true;
            
            return boot.bootPromise;
        }
        
        public BootConfig Config { get; private set; }

        public GlobalEvent AppInitialized => onAppInitialized.Value;

        [SerializeField] private GlobalEventLink onAppInitialized = Link.CreateNull<GlobalEventLink>();
        [SerializeField] private bool debugMode = false;

        public event Action OnPaused = () => { };
        public event Action<bool> OnFocused = focused => { };

        public event Action OnResumed = () => { };
        public event Action OnInitialized = () => { };

        private bool ignoreNextPauseEvent;
        private bool isRestarting;

        private void Start()
        {
            AssetCache.LoadSingletonAsset<BootConfig>().Then(bootConfig =>
            {
                Config = bootConfig;

                transform.SetParent(null);

                DontDestroyOnLoad(gameObject);

                OnAppWillBoot();

                App.InitPromise.Progress(OnLoadingProgressChanged);

                App.Create(this, GetLogger(), new ModuleFactory(), debugMode).Then(() =>
                {
                    OnInitialized();
                    OnAppInitialized();
                    bootPromise.Resolve();
                    if (onAppInitialized.HasValue)
                    {
                        onAppInitialized.Value.Invoke();
                    }
                })
                .LogException();
            }).LogException();
        }

        protected virtual ILogger GetLogger()
        {
            ILogger logger = GetComponent<ILogger>();

            if (logger == null)
            {
                logger = new UnityLogger();
            }

            return logger;
        }

        protected virtual void OnAppWillBoot()
        {
        }

        protected virtual void OnAppInitialized()
        {
        }

        protected virtual void OnAppPaused()
        {
        }

        protected virtual void OnAppResumed()
        {
        }

        protected virtual void OnAppFocusChanged(bool focused)
        {
        }

        protected virtual void OnAppWillBeRestarted()
        {
        }

        protected virtual void OnLoadingProgressChanged(float progress)
        {
        }

        public IPromise Restart()
        {
            if (isRestarting)
            {
                return bootPromise;
            }

            isRestarting = true;

            StartCoroutine(RestartRoutine());
            
            return bootPromise;
        }

        private IEnumerator RestartRoutine()
        {
            bootPromise = Promise.Create();
            OnAppWillBeRestarted();
            App.Core.Unload();
            while (IsReadyToRestart() == false)
            {
                yield return null;
            }

            Resources.UnloadUnusedAssets();
            GC.Collect();
            App.Create(this, GetLogger(), new ModuleFactory(), debugMode).Then(() =>
            {
                OnInitialized();
                OnAppInitialized();
                if (onAppInitialized.HasValue)
                {
                    onAppInitialized.Value.Invoke();
                }
                bootPromise.Resolve();
                isRestarting = false;
            })
            .LogException();
        }

        protected virtual bool IsReadyToRestart() => true;

        private void OnApplicationPause(bool pause)
        {
            if (App.Initialized == false)
            {
                return;
            }

            if (pause)
            {
                if (ignoreNextPauseEvent == false)
                {
                    OnPaused();
                    OnAppPaused();
                }
            }
            else
            {
                if (ignoreNextPauseEvent == false)
                {
                    App.Core.Timer.WaitForNextFrame().Done(() =>
                    {
                        OnResumed();
                        OnAppResumed();
                    });
                }

                App.Core.Timer.WaitForNextFrame().Done(() => ignoreNextPauseEvent = false);
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (App.Initialized == false)
            {
                return;
            }

            if (focus == false)
            {
                OnAppFocusChanged(false);
                OnFocused(false);
            }
            else
            {
                App.Core.Timer.WaitForNextFrame().Done(() =>
                {
                    OnFocused(true);
                    OnAppFocusChanged(true);
                });
            }
        }

        private void OnApplicationQuit()
        {
            if (App.Initialized == false)
            {
                return;
            }

            OnPaused();
            OnAppPaused();
        }

        public void IgnoreNextPauseEvent()
        {
            ignoreNextPauseEvent = true;
            App.Core.Timer.WaitFor(1).Done(() => { ignoreNextPauseEvent = false; });
        }
    }
}