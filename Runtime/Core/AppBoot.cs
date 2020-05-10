using System;
using System.Collections;
using UnityEngine;

namespace SwiftFramework.Core
{
    public abstract class AppBoot : MonoBehaviour
    {
        public static IPromise BootUp<T>() where T : AppBoot, IBoot
        {
            Promise bootPromise = Promise.Create();

            T boot = new GameObject(typeof(T).Name).AddComponent<T>();

            boot.OnInitialized += () =>
            {
                bootPromise.Resolve();
            };

            return bootPromise;
        }
    }

    public abstract class AppBoot<A> : AppBoot, IBoot where A : App<A>, new()
    {
        public BootConfig Config { get; private set; }

        public GlobalEvent AppInitialized => onAppInitialized.Value;

        [SerializeField] private GlobalEventLink onAppInitialized = Link.CreateNull<GlobalEventLink>();
        [SerializeField] private bool debugMode = false;

        public event Action OnPaused = () => { };
        public event Action<bool> OnFocused = focused => { };

        public event Action OnResumed = () => { };
        public event Action OnInitialized = () => { };

        private bool ignoreNextPauseEvent;

        private void Start()
        {
            AssetCache.LoadSingletonAsset<BootConfig>().Then(bootConfig => 
            {
                Config = bootConfig;

                transform.SetParent(null);

                DontDestroyOnLoad(gameObject);

                OnAppWillBoot();

                App.InitPromise.Progress(p =>
                {
                    OnLoadingProgressChanged(p);
                });

                bootConfig.modulesManifest.Load().Then(manifest =>
                {
                    App<A>.Create(this, GetLogger(), manifest, debugMode).Then(() =>
                    {
                        OnInitialized();
                        OnAppInitialized();
                        if (onAppInitialized.HasValue)
                        {
                            onAppInitialized.Value.Invoke();
                        }
                    })
                    .LogException();
                })
                .LogException();
            })
            .Catch(e => Debug.LogException(e));
        }

        protected virtual bool IsSetUpValid()
        {
            if (Config.modulesManifest.HasValue == false)
            {
                Debug.LogError($"ModuleManifest not found: {Config.modulesManifest.ToString()}");
                return false;
            }

            return true;
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

        protected virtual void OnAppWillBoot() { }

        protected virtual void OnAppInitialized() { }

        protected virtual void OnAppPaused() { }

        protected virtual void OnAppResumed() { }

        protected virtual void OnAppFocusChanged(bool focused) { }

        protected virtual void OnAppWillBeRestarted() { }

        protected virtual void OnLoadingProgressChanged(float progress) { }

        public IPromise Restart()
        {
            Promise result = Promise.Create();

            StartCoroutine(RestartRoutine(result));

            return result;
        }

        protected IEnumerator RestartRoutine(Promise result)
        {
            OnAppWillBeRestarted();
            App.Core.Unload();
            while (IsReadyToRestart() == false)
            {
                yield return null;
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
            App<A>.Create(this, GetLogger(), Config.modulesManifest.Value, debugMode).Then(() =>
            {
                OnAppInitialized();
                OnInitialized();
                result.Resolve();
            })
            .Catch(e =>
            {
                Debug.LogException(e);
            });
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
                OnAppFocusChanged(focus);
                OnFocused(focus);
            }
            else
            {
                App.Core.Timer.WaitForNextFrame().Done(() =>
                {
                    OnFocused(focus);
                    OnAppFocusChanged(focus);
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
            App.Core.Timer.WaitFor(1).Done(() =>
            {
                ignoreNextPauseEvent = false;
            });
        }
    }
}
