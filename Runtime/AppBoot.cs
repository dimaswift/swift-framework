using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        public GlobalEvent AppInitialized => onAppInitialized.Value;

        [SerializeField] private float bootDelay = 0.5f;
        [SerializeField] private GlobalEventLink onAppInitialized = Link.CreateNull<GlobalEventLink>();
        [SerializeField] private bool debugMode = false;

        public event Action OnPaused = () => { };
        public event Action<bool> OnFocused = focused => { };

        public event Action OnResumed = () => { };
        public event Action OnInitialized = () => { };

        private BootConfig bootConfig;

        public virtual GlobalConfig GlobalConfig => bootConfig.globalConfig.Value;

        private bool ignoreNextPauseEvent;

        private IEnumerator Start()
        {
            var bootConfigPath = "Configs/BootConfig";
            var bootConfigLoad = Addressables.LoadAssetAsync<BootConfig>(bootConfigPath);

            while (bootConfigLoad.IsDone == false)
            {
                yield return null;
            }

            bootConfig = bootConfigLoad.Result;

            if (bootConfig == null)
            {
                Debug.Log($"Cannot load BootConfig. Make sure it is present in the project inside Assets/{bootConfigPath}");
                yield break;
            }

            yield return new WaitForSeconds(bootDelay);

            if (IsSetUpValid() == false)
            {
                Debug.LogError("Setup is invalid.");
                yield break;
            }

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
        }

        protected virtual bool IsSetUpValid()
        {
            if (bootConfig.modulesManifest.HasValue == false)
            {
                Debug.LogError($"ModuleManifest not found: {bootConfig.modulesManifest.ToString()}");
                return false;
            }

            if (bootConfig.globalConfig.HasValue == false)
            {
                Debug.LogError($"GlobalConfig not found: {bootConfig.globalConfig.ToString()}");
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
            App<A>.Create(this, GetLogger(), bootConfig.modulesManifest.Value, debugMode).Then(() =>
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
